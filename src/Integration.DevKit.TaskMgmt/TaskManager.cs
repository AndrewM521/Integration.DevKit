using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using Integration.DevKit.Core;
using Integration.DevKit.Core.Logging;
using Integration.DevKit.TaskMgmt.Settings;
using Integration.DevKit.TaskMgmt.Models;
using Integration.DevKit.TaskMgmt.Abstractions;
using Integration.DevKit.TaskMgmt.Interfaces;
using System.Runtime.ExceptionServices;

namespace Integration.DevKit.TaskMgmt;

/// <summary>
/// Core manager responsible for the lifecycle,
/// execution, and monitoring of managed tasks.
/// </summary>
public class TaskManager
{
    /// <summary>
    /// Gets the current active configuration and limits for the task manager.
    /// </summary>
    /// <value>A <see cref="TaskManagerSettings"/> instance containing concurrency limits and registry capacity settings.</value>
    public TaskManagerSettings RuntimeSettings { get; init; }

    private readonly ConcurrentDictionary<string, ManagedTaskRuntime> _tasks = new ConcurrentDictionary<string, ManagedTaskRuntime>();
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly ILogger? _logger;
    private SemaphoreSlim _taskLimiter = null!;
    private readonly TaskRegistryRuntime _taskRegistryRuntime;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskManager"/> class.
    /// Configures concurrency limits and registers a global cancellation callback with the 
    /// <see cref="IHostApplicationLifetime"/> to ensure graceful shutdown of all managed tasks.
    /// </summary>
    /// <param name="appLifetime">The application lifetime for monitoring host shutdown events.</param>
    /// <param name="loggerFactory">The factory used to resolve the internal logger.</param>
    /// <param name="taskRegistry">The registry used for persisting and tracking task states.</param>
    /// <param name="settings">The configuration settings for task management and limits.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="appLifetime"/> or <paramref name="taskRegistry"/> is null.</exception>
    public TaskManager(
        IHostApplicationLifetime appLifetime,
        TaskRegistry taskRegistry,
        IOptions<TaskManagerSettings> settings,
        ILoggerFactory? loggerFactory = null)
    {
        if (appLifetime == null)
        {
            throw new ArgumentNullException(nameof(appLifetime));
        }

        if (taskRegistry == null)
        {
            throw new ArgumentNullException(nameof(taskRegistry));
        }

        _appLifetime = appLifetime;
        _appLifetime.ApplicationStopping.Register(() =>
        {
            var cancelResult = CancelAllTasks();
            if (cancelResult.MethodSuccess)
            {
                _logger?.LogInformation($"[{nameof(TaskManager)}] All tasks called to cancel during host shutdown.");
            }
            else
            {
                _logger?.LogError(cancelResult.Exception, $"[{nameof(TaskManager)}] Failed to cancel all tasks during host shutdown.");
            }
        });

        RuntimeSettings = settings.Value.Clone();

        _logger = loggerFactory?.CreateConditionalLogger(nameof(TaskManager), () => RuntimeSettings.EnableLogging);
        _taskRegistryRuntime = new TaskRegistryRuntime(this, taskRegistry, loggerFactory);

        Initialize();
    }

    /// <summary>
    /// Re-derives everything this manager caches from <see cref="RuntimeSettings"/> (currently the
    /// concurrent-task rate limiter). Call this after mutating <see cref="RuntimeSettings"/> in place
    /// so the change takes effect.
    /// </summary>
    /// <returns>A <see cref="NullOperationResult"/> indicating success or failure.</returns>
    /// <remarks>
    /// Re-derives everything this manager caches from <see cref="RuntimeSettings"/> at construction time
    /// (currently the concurrent-task rate limiter). Call this after mutating <see cref="RuntimeSettings"/>
    /// in place so the change takes effect.
    /// </remarks>
    public NullOperationResult Initialize()
    {
        var result = new NullOperationResult();

        try
        {
            // 0 would make a semaphore with no slots, so every task would wait forever
            if (RuntimeSettings.MaxConcurrentTasks <= 0)
            {
                RuntimeSettings.MaxConcurrentTasks = int.MaxValue;
            }

            if (RuntimeSettings.MaxTaskRegistryCount < 0)
            {
                RuntimeSettings.MaxTaskRegistryCount = int.MaxValue;
            }

            if (RuntimeSettings.MaxTaskIterationRegistryCount < 0)
            {
                RuntimeSettings.MaxTaskIterationRegistryCount = int.MaxValue;
            }

            // Not disposed: running tasks still release the limiter they acquired, and waiters on it would fault.
            // SemaphoreSlim only needs disposing when AvailableWaitHandle is used, which it isn't here.
            _taskLimiter = new SemaphoreSlim(RuntimeSettings.MaxConcurrentTasks);

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Initiates a task and tracks it within the manager.
    /// </summary>
    /// <param name="managedTask">The task implementation to be executed.</param>
    /// <param name="executionMode">Specifies whether the task should run synchronously or asynchronously.</param>
    /// <param name="settings">Execution-specific settings for this task instance.</param>
    /// <param name="cancellationToken">An external token to monitor for cancellation requests.</param>
    /// <returns>
    /// An <see cref="OperationResult{IManagedTaskHandle}"/> containing the handle to the started task if successful;
    /// otherwise, a failure result explaining why the task could not be initiated.
    /// </returns>
    /// <exception cref="InvalidOperationException">Thrown if a task with the same key is already active.</exception>
    public async Task<OperationResult<IManagedTaskHandle>> StartTask(ManagedTask managedTask, TaskExecutionMode executionMode, ManagedTaskSettings? settings = null, CancellationToken cancellationToken = default)
    {
        var result = new OperationResult<IManagedTaskHandle>();

        ManagedTaskRuntime? managedTaskRuntime = null;

        try
        {
            if (managedTask == null)
            {
                throw new ArgumentNullException(nameof(managedTask));
            }

            var runtimeSettings = new ManagedTaskSettings();
            if (settings != null)
            {
                runtimeSettings = settings;
            }

            // Nothing would ever make the iteration loop wait: it would start DoTaskWork without end until memory runs out
            bool runsIterationsInParallel = runtimeSettings.IterationExecutionMode == ManagedTaskExecutionMode.Parallel || runtimeSettings.AllowParallelIterationExecution;
            if (runsIterationsInParallel &&
                runtimeSettings.MaxConcurrentParallelTasks <= 0 &&
                runtimeSettings.MaxIterations <= 0 &&
                runtimeSettings.IterationStrategy.GetType() == typeof(BaseIterationStrategy))
            {
                throw new ArgumentException(
                    $"Task '{managedTask.TaskKey}' runs iterations in parallel with no concurrency limit, no iteration limit and no iteration strategy, so it would start iterations without end. " +
                    $"Set {nameof(ManagedTaskSettings.MaxConcurrentParallelTasks)}, {nameof(ManagedTaskSettings.MaxIterations)} or an {nameof(ManagedTaskSettings.IterationStrategy)}.");
            }

            var newRuntime = new ManagedTaskRuntime(managedTask, runtimeSettings, cancellationToken);

            // Locked so two starts can't both pass the shared-strategy check before either is added
            lock (_tasks)
            {
                // A time strategy remembers its last run, so two tasks sharing one instance each skip every other slot
                bool isStrategyShared = false;

                if (runtimeSettings.IterationStrategy is Time_IterationStrategy)
                {
                    foreach (var runningTask in _tasks.Values)
                    {
                        if (runningTask.UserTask.TaskKey != managedTask.TaskKey && ReferenceEquals(runningTask.RuntimeSettings.IterationStrategy, runtimeSettings.IterationStrategy))
                        {
                            isStrategyShared = true;
                            break;
                        }
                    }
                }

                if (isStrategyShared)
                {
                    newRuntime.Dispose();

                    throw new InvalidOperationException($"The iteration strategy given to task '{managedTask.TaskKey}' is already used by another running task. Give each task its own strategy instance.");
                }

                if (!_tasks.TryAdd(managedTask.TaskKey, newRuntime))
                {
                    // Leave the running task's entry and snapshot alone; only this unused runtime is discarded
                    newRuntime.Dispose();

                    throw new InvalidOperationException($"A managed task with key '{managedTask.TaskKey}' is already running.");
                }
            }

            managedTaskRuntime = newRuntime;

            managedTaskRuntime.State = ManagedTaskState.Starting;

            var upsert = _taskRegistryRuntime.Upsert(managedTaskRuntime);
            if (!upsert.MethodSuccess)
            {
                ExceptionDispatchInfo.Throw(upsert.Exception);
            }

            managedTaskRuntime.LifecycleTask = RunManagedTaskAsync(managedTaskRuntime);

            if (executionMode == TaskExecutionMode.Syncronous)
            {
                await managedTaskRuntime.LifecycleTask.ConfigureAwait(false);
            }

            return result.SetMethodSuccess(managedTaskRuntime.Handle);
        }
        catch (Exception ex)
        {
            if (managedTaskRuntime != null)
            {
                _tasks.TryRemove(new KeyValuePair<string, ManagedTaskRuntime>(managedTask.TaskKey, managedTaskRuntime));

                managedTaskRuntime.State = ManagedTaskState.Faulted;

                var upsert = _taskRegistryRuntime.Upsert(managedTaskRuntime);
                if (!upsert.MethodSuccess)
                {
                    ExceptionDispatchInfo.Throw(upsert.Exception);
                }
            }

            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Requests the cancellation of a specific task.
    /// </summary>
    /// <param name="taskKey">The unique identifier of the task to cancel.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating success or failure of the cancellation request.</returns>
    public NullOperationResult CancelTask(string taskKey)
    {
        var result = new NullOperationResult();

        try
        {
            if (string.IsNullOrWhiteSpace(taskKey))
            {
                throw new ArgumentException("Task key cannot be null or whitespace.");
            }

            if (_tasks.TryGetValue(taskKey, out var managedTaskRuntime))
            {
                managedTaskRuntime.Cancel();

                var upsert = _taskRegistryRuntime.Upsert(managedTaskRuntime);
                if (!upsert.MethodSuccess)
                {
                    ExceptionDispatchInfo.Throw(upsert.Exception);
                }
            }

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Requests the cancellation of all currently tracked tasks.
    /// </summary>
    /// <returns>A <see cref="NullOperationResult"/> indicating the overall outcome of the bulk cancellation.</returns>
    public NullOperationResult CancelAllTasks()
    {
        var result = new NullOperationResult();
        var errors = new List<Exception>();

        foreach (var taskKey in _tasks.Keys.ToList())
        {
            var cancelTask = CancelTask(taskKey);
            if (!cancelTask.MethodSuccess)
            {
                errors.Add(cancelTask.Exception);
            }
        }

        if (errors.Count > 0)
        {
            return result.SetMethodFailure(new AggregateException(errors));
        }

        return result.SetMethodSuccess();
    }

    /// <summary>
    /// Checks whether a task associated with the specified key is currently in a running state.
    /// </summary>
    /// <param name="taskKey">The unique identifier of the task.</param>
    /// <returns>
    /// An <see cref="OperationResult{Boolean}"/> where the value is <see langword="true"/> if the task exists and is active.
    /// </returns>
    public OperationResult<bool> IsTaskRunning(string taskKey)
    {
        var result = new OperationResult<bool>();

        try
        {
            bool isRunning = false;

            if (_tasks.TryGetValue(taskKey, out var managedTask))
            {
                if (managedTask.State == ManagedTaskState.Starting ||
                    managedTask.State == ManagedTaskState.Running)
                {
                    isRunning = true;
                }
            }

            return result.SetMethodSuccess(isRunning);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Retrieves a collection of keys for all tasks currently managed and running.
    /// </summary>
    /// <returns>An enumerable of task key strings.</returns>
    public IEnumerable<string> GetAllRunningTaskKeys()
    {
        return _tasks.Keys;
    }


    #region Helpers
    private async Task RunManagedTaskAsync(ManagedTaskRuntime managedTaskRuntime)
    {
        ConcurrentQueue<Exception> exceptions = new ConcurrentQueue<Exception>();
        // Only this loop touches it. Holds iterations the loop doesn't wait for, so shutdown can wait for them.
        List<Task> parallelIterations = new List<Task>();

        var runtimeSettings = managedTaskRuntime.RuntimeSettings;
        var taskHandle = managedTaskRuntime.Handle;
        var taskKey = managedTaskRuntime.UserTask.TaskKey;

        // Captured so a concurrent Initialize() swapping the limiter can't make this release the wrong one
        SemaphoreSlim taskLimiter = _taskLimiter;

        try
        {
            await taskLimiter.WaitAsync();

            managedTaskRuntime.StartDTM = DateTime.UtcNow;
            managedTaskRuntime.State = ManagedTaskState.Running;

            var upsert = _taskRegistryRuntime.Upsert(managedTaskRuntime);
            if (!upsert.MethodSuccess)
            {
                ExceptionDispatchInfo.Throw(upsert.Exception);
            }

            ManagedTaskIterationHandle? lastIterationHandle = null;
            while (!managedTaskRuntime._lifecycleCTS.Token.IsCancellationRequested)
            {
                // Wait for this task to be ready to run its next iteration
                await runtimeSettings.IterationStrategy.WaitForReadyAsync(taskHandle!, managedTaskRuntime._lifecycleCTS.Token, _logger);

                // Check if we should actually start another one
                if (!ShouldContinueIterating(managedTaskRuntime))
                {
                    break;
                }

                // Determine if we need to WAIT or OVERLAP
                if (runtimeSettings.IterationExecutionMode == ManagedTaskExecutionMode.Sequential)
                {
                    if (lastIterationHandle != null && lastIterationHandle.IsRunning)
                    {
                        string runningLongMsg = $"Task '{taskKey}' iteration {lastIterationHandle.IterationNumber} running longer than Iteration Strategy.";
                        if (runtimeSettings.AllowParallelIterationExecution)
                        {
                            _logger?.LogWarning(runningLongMsg + $" Creating new parallel iteration due to execution policy. ({nameof(runtimeSettings.AllowParallelIterationExecution)})");
                        }
                        else
                        {
                            _logger?.LogWarning(runningLongMsg);
                        }
                    }
                }

                // Wait for a concurrency slot here, not inside the iteration. Otherwise nothing in this loop ever waits,
                // so a parallel task with no iteration limit spins on the caller's thread and queues iterations without end.
                try
                {
                    await managedTaskRuntime._concurrencyLock.WaitAsync(managedTaskRuntime._lifecycleCTS.Token);
                }
                catch (OperationCanceledException)
                {
                    // Break instead of throwing, so the iterations already running are still awaited below
                    break;
                }

                // Create the runtime now before creating the parallelTask. This needs to be called on this thread so that the
                // iteration count increases immediatly. If not a race condition will happen where this while loop goes past
                // the MaxIteration count since its not increased each time this while loop goes
                var iterationRuntime = managedTaskRuntime.CreateIterationRuntime();
                lastIterationHandle = iterationRuntime.IterationHandle;

                var iterationTask = Task.Run(async () =>
                {
                    try
                    {
                        await RunTaskIteration(managedTaskRuntime, iterationRuntime, exceptions);
                    }
                    finally
                    {
                        // The slot was taken by the loop above
                        managedTaskRuntime._concurrencyLock.Release();

                        iterationRuntime.Dispose();
                    }
                });
                
                // We only block the loop if we are in Sequential mode without parallel execution.
                if (runtimeSettings.IterationExecutionMode == ManagedTaskExecutionMode.Sequential && !runtimeSettings.AllowParallelIterationExecution)
                {
                    await iterationTask;
                }
                else
                {
                    // Drop the ones that finished cleanly, or a long-running task keeps every iteration it ever ran.
                    // Faulted or canceled ones stay so the WhenAll below still reports them.
                    parallelIterations.RemoveAll(t => t.IsCompletedSuccessfully);
                    parallelIterations.Add(iterationTask);
                }
            }

            if (parallelIterations.Count > 0)
            {
                _logger?.LogDebug($"Task '{taskKey}' waiting for {parallelIterations.Count} iterations to finish...");

                // We do an WhenAll here to wait for all parallelTasks to finish. This negates the need to check if any tasks are still running
                await Task.WhenAll(parallelIterations);
            }

            // Wait a moment for processes to finish when the task completes
            await Task.Delay(50);

            // If we exited the loop because of cancellation and no other state has been set, ensure we mark it as Canceled.
            if (managedTaskRuntime._lifecycleCTS.Token.IsCancellationRequested && 
                (managedTaskRuntime.State == ManagedTaskState.Running || managedTaskRuntime.State == ManagedTaskState.CancelRequested))
            {
                managedTaskRuntime.State = ManagedTaskState.Canceled;
            }
        }
        catch (OperationCanceledException)
        {
            // This is expected during shutdown
            managedTaskRuntime.State = ManagedTaskState.Canceled;
        }
        catch (Exception ex)
        {
            RecordException(exceptions, ex);

            managedTaskRuntime.State = ManagedTaskState.Faulted;

            _logger?.LogError(ex, $"Task '{taskKey}' threw an unexpected exception.");
        }
        finally 
        {
            managedTaskRuntime.EndDTM = DateTime.UtcNow;

            // Remove from the active dictionary
            _tasks.TryRemove(taskKey, out _);

            // Combine all errors from all parallel/sequential iterations
            Exception? finalEx = null;
            if (exceptions.Count > 0)
            {
                //This needs to set the state to Faulted because the main Task may not have faulted but a iteration may have.
                managedTaskRuntime.State = ManagedTaskState.Faulted;

                finalEx = new AggregateException(exceptions);
            }

            if (managedTaskRuntime.State != ManagedTaskState.Canceled &&
                managedTaskRuntime.State != ManagedTaskState.Faulted)
            {
                managedTaskRuntime.State = ManagedTaskState.Completed;
            }

            // Final Registry Update
            // Log instead of throwing, so the global slot below is always released.
            var upsert = _taskRegistryRuntime.Upsert(managedTaskRuntime, null, finalEx);
            if (!upsert.MethodSuccess)
            {
                _logger?.LogError(upsert.Exception, $"Task '{taskKey}' final registry update failed.");
            }

            // Release the global slot
            taskLimiter.Release();

            // Clean up the runtime resources
            managedTaskRuntime.Dispose();
        }
    }

    private async Task<Task?> RunTaskIteration(ManagedTaskRuntime managedTaskRuntime, ManagedTaskIterationRuntime  iterationRuntime, ConcurrentQueue<Exception> centralExceptions)
    {
        Task? workerTask = null;

        int retryCount = 0;
        bool firstRun = true;
        bool needRetry = false;

        var runtimeSettings = managedTaskRuntime.RuntimeSettings;
        var linkedToken = iterationRuntime.Token;
        var taskKey = managedTaskRuntime.UserTask.TaskKey;

        iterationRuntime.StartDTM = DateTime.UtcNow;
        iterationRuntime.State = ManagedTaskState.Running;

        var upsert = _taskRegistryRuntime.Upsert(managedTaskRuntime, iterationRuntime);
        if (!upsert.MethodSuccess)
        {
            ExceptionDispatchInfo.Throw(upsert.Exception);
        }

        // Once per iteration, not per attempt: each one stays alive until the iteration ends
        Task cancelWatchdog = Task.Run(async() => {
            //Watch for the cancelation token call. Caught, or the throw skips the grace delay below.
            try
            {
                await Task.Delay(Timeout.Infinite, linkedToken);
            }
            catch (OperationCanceledException)
            {
            }

            //Wait a small amount of time to prevent a false positive "still running" warning
            await Task.Delay(1000);
        });

        while (!linkedToken.IsCancellationRequested && (firstRun || needRetry))
        {
            firstRun = false;
            needRetry = false;

            // Reset per attempt so a retry never re-evaluates the previous attempt's task
            workerTask = null;

            try
            {
                // 1. Start the actual work
                workerTask = managedTaskRuntime.UserTask.DoTaskWork(iterationRuntime.IterationHandle);

                // 3. Setup the Timeout Watchdog if a timeout is defined
                Task? timeoutWatchdog = null;
                if (runtimeSettings.Timeout.HasValue)
                {
                    timeoutWatchdog = Task.Run(async() => {
                        try
                        {
                            await Task.Delay(runtimeSettings.Timeout.Value, linkedToken);
                        }
                        catch (TaskCanceledException)
                        {
                            //If the linked token is canceled it will wait for the cancelWatchdog to finish.
                            //This is so that if the task has a timeout set and the task is canceled, it will 
                            //will not throw a "timeout triggered" message before the actual cancel finishes
                            await cancelWatchdog;
                        }
                    }); 
                }

                List<Task> tasksToWatch = new List<Task> { workerTask!, cancelWatchdog };
                if (timeoutWatchdog != null)
                {
                    tasksToWatch.Add(timeoutWatchdog);
                }

                // Wait for the first to complete
                Task completedTask = await Task.WhenAny(tasksToWatch);

                if (completedTask == timeoutWatchdog)
                {
                    _logger?.LogWarning($"Task '{taskKey}' iteration {iterationRuntime.IterationNumber} timeout triggered.");

                    iterationRuntime.Cancel();
                }

                if (completedTask != workerTask && managedTaskRuntime._lifecycleCTS.IsCancellationRequested)
                {
                    // Warn if the main task is still running unexpectedly
                    // NOTE: In .NET, there is no built-in way to forcibly terminate a Task from outside. 
                    // Even though we stopped awaiting it (via force cancel or watchdog), the task may still be executing.
                    // This can happen if the task ignored cancellation requests or is stuck in a blocking operation.
                    // Logging this is crucial because it may continue consuming resources or causing unintended side effects.

                    _logger?.LogWarning($"Task '{taskKey}' iteration {iterationRuntime.IterationNumber} running longer than 1 second after being canceled. Are you checking the iteration cancelation token?");
                }
            }
            catch (TaskCanceledException)
            {
                // Catch TaskCanceledException so its not concidered an error, but State is changed below
            }
            catch (Exception ex)
            {
                // DoTaskWork threw before returning a Task. Wrap it so the faulted branch below logs, records and retries it.
                if (workerTask == null)
                {
                    workerTask = Task.FromException(ex);
                }
            }
            finally
            {
                // Capture EndTime for this handle
                iterationRuntime.EndDTM = DateTime.UtcNow;
            }

            // Wait a moment for processes to finish when the task completes
            await Task.Delay(50);

            // 4. Evaluate Results & Exceptions
            Exception? iterationException = null;

            if (workerTask != null)
            {
                if (workerTask.IsCompleted)
                {
                    if (workerTask.IsFaulted)
                    {
                        iterationRuntime.State = ManagedTaskState.Faulted;

                        iterationException = new Exception("Unknown worker exception");

                        if (workerTask.Exception != null)
                        {
                            iterationException = workerTask.Exception.InnerException;
                        }

                        // Add to the central queue (for the final Registry update)
                        RecordException(centralExceptions, iterationException!);

                        _logger?.LogError(iterationException, $"Task '{taskKey}' iteration {iterationRuntime.IterationNumber} faulted.");

                        // --- RETRY LOGIC ---
                        if (runtimeSettings.RetryOnException)
                        {
                            if (runtimeSettings.MaxRetryCount == -1 || retryCount < runtimeSettings.MaxRetryCount)
                            {
                                retryCount++;
                                needRetry = true;

                                _logger?.LogWarning($"Task '{taskKey}' iteration {iterationRuntime.IterationNumber} retrying... (attempt {retryCount}).");
                            }
                            else
                            {
                                _logger?.LogWarning($"Task '{taskKey}' iteration {iterationRuntime.IterationNumber} retry limit reached.");

                                if (runtimeSettings.StopIterationAfterMaxRetries)
                                {
                                    _logger?.LogWarning($"Task '{taskKey}' stopped due to execution policy. ({nameof(runtimeSettings.StopIterationAfterMaxRetries)})");

                                    managedTaskRuntime.Cancel(); // Kills the lifecycle
                                }
                            }
                        }
                        else if (runtimeSettings.StopIteratingOnException)
                        {
                            _logger?.LogWarning($"Task '{taskKey}' stopped due to execution policy. ({nameof(runtimeSettings.StopIteratingOnException)})");

                            // No break: the canceled token ends the loop after the registry update below records the fault.
                            managedTaskRuntime.Cancel(); // Kills the lifecycle
                        }
                    }
                    else if (workerTask.IsCanceled)
                    {
                        iterationRuntime.State = ManagedTaskState.Canceled;

                        _logger?.LogTrace($"Task '{taskKey}' iteration {iterationRuntime.IterationNumber} was cancelled.");
                    }
                    else
                    {
                        iterationRuntime.State = ManagedTaskState.Completed;

                        _logger?.LogTrace($"Task '{taskKey}' iteration {iterationRuntime.IterationNumber} completed sucessfully.");
                    }
                }
                else
                {
                    iterationRuntime.State = ManagedTaskState.Canceled;
                }
            }

            var upsert1 = _taskRegistryRuntime.Upsert(managedTaskRuntime, iterationRuntime, null, iterationException);
            if (!upsert1.MethodSuccess)
            {
                ExceptionDispatchInfo.Throw(upsert1.Exception);
            }

            if (needRetry)
            {
                // Back off 1s, 2s, 4s ... up to 60s so a broken dependency isn't hit 20 times a second
                TimeSpan backoff = TimeSpan.FromSeconds(Math.Min(60, 1 << Math.Min(retryCount - 1, 6)));

                try
                {
                    await Task.Delay(backoff, linkedToken);
                }
                catch (OperationCanceledException)
                {
                    // Canceled while waiting; the loop condition ends the retries
                }
            }
        }

        return workerTask;
    }

    
    /// <summary>
    /// Adds an exception to the task's list, dropping the oldest ones once it holds more than the iteration history keeps.
    /// </summary>
    private void RecordException(ConcurrentQueue<Exception> exceptions, Exception ex)
    {
        exceptions.Enqueue(ex);

        // Bounded like the iteration history (at least 1, so a fault is never lost), or a task that keeps failing grows memory forever
        int maxExceptions = Math.Max(1, RuntimeSettings.MaxTaskIterationRegistryCount);

        while (exceptions.Count > maxExceptions)
        {
            // Parallel iterations trim the same queue, so stop if another one already took the last extra item
            if (!exceptions.TryDequeue(out _))
            {
                break;
            }
        }
    }

    /// <summary>
    /// Evaluates if a task should proceed to its next scheduled iteration.
    /// </summary>
    private bool ShouldContinueIterating(ManagedTaskRuntime managedTaskRuntime)
    {
        var runtimeSettings = managedTaskRuntime.RuntimeSettings;

        if (managedTaskRuntime._lifecycleCTS.IsCancellationRequested)
        {
            return false;
        }

        if (runtimeSettings.MaxIterations > 0)
        {
            if (managedTaskRuntime.IterationCount >= runtimeSettings.MaxIterations)
            {
                return false;
            }
        }

        return true;
    }
    #endregion
}


/// <summary>
/// Defines the threading behavior for a task.
/// </summary>
public enum TaskExecutionMode
{
    /// <summary>
    /// The task runs on a background thread, allowing the caller to continue immediately.
    /// </summary>
    Asyncronous,
    /// <summary>
    /// The task runs on the current thread, blocking the caller until completion.
    /// </summary>
    Syncronous
}

/// <summary>
/// Represents the various lifecycle stages of a managed task.
/// </summary>
public enum ManagedTaskState
{
    /// <summary> The task has been initialized but not yet started. </summary>
    Idle,
    /// <summary> The task is currently undergoing initialization and setup. </summary>
    Starting,
    /// <summary> The task is actively executing its workload. </summary>
    Running,
    /// <summary> The task has finished successfully. </summary>
    Completed,
    /// <summary> The task was stopped by a user or system request. </summary>
    Canceled,
    /// <summary> The task stopped due to an unhandled exception or error. </summary>
    Faulted,
    /// <summary> A cancellation has been signaled, but the task has not yet stopped. </summary>
    CancelRequested
}
