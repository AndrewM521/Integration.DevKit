using AndrewM5.DevKit.Core;
using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.Logging.Contracts.Interfaces;
using AndrewM5.DevKit.TaskManagement.Contracts.Interfaces;
using AndrewM5.DevKit.TaskManagement.Contracts.Models;
using AndrewM5.DevKit.TaskManagement.Contracts.Options;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;

namespace AndrewM5.DevKit.TaskManagement;

/// <summary>
/// The central coordinator for the task management system. 
/// Handles task initiation, concurrency limiting, lifecycle monitoring, and host shutdown integration.
/// </summary>
public class TaskManager : ITaskManager
{
    /// <inheritdoc/>
    public TaskManagerSettings RuntimeSettings { get; init; }

    private readonly ConcurrentDictionary<string, ManagedTaskRuntime> _tasks = new ConcurrentDictionary<string, ManagedTaskRuntime>();
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly ICustomLogger _logger;

    /// <summary>
    /// Limits the number of concurrent tasks allowed to run based on <see cref="TaskManagerSettings.MaxConcurrentTasks"/>.
    /// </summary>
    private readonly SemaphoreSlim _taskLimiter;
    private readonly TaskRegistryRuntime _taskRegistryRuntime;

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskManager"/>.
    /// Hooks into <see cref="IHostApplicationLifetime"/> to ensure all tasks are signaled for cancellation during shutdown.
    /// </summary>
    public TaskManager(
        IHostApplicationLifetime appLifetime,
        ICustomLoggerManager loggerManager,
        ITaskRegistry taskRegistry,
        IOptions<TaskManagerSettings> settings)
    {
        if (appLifetime == null)
        {
            throw new ArgumentNullException(nameof(appLifetime));
        }

        if (loggerManager == null)
        {
            throw new ArgumentNullException(nameof(loggerManager));
        }

        if (taskRegistry == null)
        {
            throw new ArgumentNullException(nameof(taskRegistry));
        }

        _appLifetime = appLifetime;
        _appLifetime.ApplicationStopping.Register(() =>
        {
            var cancelResult = CancelAllTasks(true);
            if (cancelResult.MethodSuccess)
            {
                _logger?.LogInformation($"[{nameof(TaskManager)}] All tasks called to cancel during host shutdown.");
            }
            else
            {
                _logger?.LogError($"[{nameof(TaskManager)}] {cancelResult.Exception.Message}");
            }
        });

        _logger = loggerManager.GetLogger("TaskManager");
        _taskRegistryRuntime = new TaskRegistryRuntime(this, taskRegistry);

        RuntimeSettings = settings.Value.Clone();

        if (RuntimeSettings.MaxConcurrentTasks < 0)
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

        _taskLimiter = new SemaphoreSlim(RuntimeSettings.MaxConcurrentTasks);   
    }

    /// <inheritdoc/>
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

            managedTaskRuntime = new ManagedTaskRuntime(managedTask, runtimeSettings, cancellationToken);

            if (!_tasks.TryAdd(managedTask.TaskKey, managedTaskRuntime))
            {
                throw new InvalidOperationException($"A managed task with key '{managedTask.TaskKey}' is already running.");
            }

            managedTaskRuntime.State = ManagedTaskState.Starting;

            var upsert = _taskRegistryRuntime.Upsert(managedTaskRuntime);
            if (!upsert.MethodSuccess)
            {
                throw upsert.Exception;
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
                _tasks.TryRemove(managedTask.TaskKey, out _);

                managedTaskRuntime.State = ManagedTaskState.Faulted;

                var upsert = _taskRegistryRuntime.Upsert(managedTaskRuntime);
                if (!upsert.MethodSuccess)
                {
                    throw upsert.Exception;
                }
            }

            return result.SetMethodFailure(ex);
        }
    }

    /// <inheritdoc/>
    public NullOperationResult CancelTask(string taskKey, bool forceCancel = false)
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
                    throw upsert.Exception;
                }
            }

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <inheritdoc/>
    public NullOperationResult CancelAllTasks(bool forceCancel = false)
    {
        var result = new NullOperationResult();
        var errors = new List<Exception>();

        foreach (var taskKey in _tasks.Keys.ToList())
        {
            var cancelTask = CancelTask(taskKey, forceCancel);
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public IEnumerable<string> GetAllRunningTaskKeys()
    {
        return _tasks.Keys;
    }

    /// <inheritdoc/>
    public async Task AwaitAllTasksToFinish(List<Task> tasksList)
    {
        await Task.WhenAll(tasksList).ConfigureAwait(false);
    }

    /// <inheritdoc/>
    public void OutputRuntimeSettings()
    {
        _logger?.LogDebug($"--- Task Manager Settings ---");

        Type type = RuntimeSettings.GetType();
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            object? value = property.GetValue(RuntimeSettings);
            _logger?.LogDebug($"  {property.Name}: {value}");
        }
    }


    #region Helpers
    /// <summary>
    /// The core execution engine for a managed task. 
    /// Manages scheduling (via NextRunStrategy), iteration loops, linked cancellation tokens, 
    /// timeouts (watchdogs), and retry logic.
    /// </summary>
    private async Task RunManagedTaskAsync(ManagedTaskRuntime managedTaskRuntime)
    {
        ConcurrentQueue<Exception> exceptions = new ConcurrentQueue<Exception>();
        ConcurrentBag<Task> parallelIterations = new ConcurrentBag<Task>();

        var runtimeSettings = managedTaskRuntime.RuntimeSettings;
        var taskHandle = managedTaskRuntime.Handle;
        var taskKey = managedTaskRuntime.UserTask.TaskKey;

        try
        {
            await _taskLimiter.WaitAsync();


            managedTaskRuntime.StartDTM = DateTime.UtcNow;
            managedTaskRuntime.State = ManagedTaskState.Running;

            var upsert = _taskRegistryRuntime.Upsert(managedTaskRuntime);
            if (!upsert.MethodSuccess)
            {
                throw upsert.Exception;
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
                            _logger?.LogDebug(runningLongMsg + $" Creating new parallel iteration due to execution policy. ({nameof(runtimeSettings.AllowParallelIterationExecution)})");
                        }
                        else
                        {
                            _logger?.LogDebug(runningLongMsg);
                        }
                    }
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
                        // Wait for a concurrency slot to open up
                        await managedTaskRuntime._concurrencyLock.WaitAsync(managedTaskRuntime._lifecycleCTS.Token);

                        try
                        {
                            await RunTaskIteration(managedTaskRuntime, iterationRuntime, exceptions);
                        }
                        finally
                        {
                            managedTaskRuntime._concurrencyLock.Release();
                        }
                    }
                    finally
                    {
                        iterationRuntime.Dispose();
                    }
                });
                
                parallelIterations.Add(iterationTask);

                // We only block the loop if we are in Sequential mode without parallel execution.
                if (runtimeSettings.IterationExecutionMode == ManagedTaskExecutionMode.Sequential && !runtimeSettings.AllowParallelIterationExecution)
                {
                    await iterationTask;
                }
            }

            if (!parallelIterations.IsEmpty)
            {
                _logger?.LogDebug($"Task '{taskKey}' waiting for {parallelIterations.Count} iterations to finish...");

                // We do an WhenAll here to wait for all parallelTasks to finish. This negates the need to check if any tasks are still running
                await Task.WhenAll(parallelIterations);
            }

            // Wait a moment for processes to finish when the task completes
            await Task.Delay(50);

            // If we exited the loop because of cancellation and no other state has been set, ensure we mark it as Canceled.
            if (managedTaskRuntime._lifecycleCTS.Token.IsCancellationRequested &&
                managedTaskRuntime.State == ManagedTaskState.Running)
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
            exceptions.Enqueue(ex);

            managedTaskRuntime.State = ManagedTaskState.Faulted;

            _logger?.LogError($"Task '{taskKey}' threw an unexpected exception: {ex.Message}");
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
                managedTaskRuntime.State = ManagedTaskState.Faulted;

                finalEx = new AggregateException(exceptions);
            }

            // Final Registry Update
            var upsert = _taskRegistryRuntime.Upsert(managedTaskRuntime, null, finalEx);
            if (!upsert.MethodSuccess)
            {
                throw upsert.Exception;
            }

            // Release the global slot
            _taskLimiter.Release();

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
            throw upsert.Exception;
        }

        while (!linkedToken.IsCancellationRequested && (firstRun || needRetry))
        {
            firstRun = false;
            needRetry = false;

            try
            {
                // 1. Start the actual work
                workerTask = managedTaskRuntime.UserTask.DoTaskWork(iterationRuntime.IterationHandle);

                // 2. Setup the Timeout Watchdog if a timeout is defined
                Task? timeoutWatchdog = null;
                if (managedTaskRuntime.UserTask.Timeout.HasValue)
                {
                    timeoutWatchdog = Task.Delay(managedTaskRuntime.UserTask.Timeout.Value, linkedToken);
                }

                List<Task> tasksToWatch = new List<Task> { workerTask! };
                if (timeoutWatchdog != null)
                {
                    tasksToWatch.Add(timeoutWatchdog);
                }

                // Wait for the first to complete
                Task completedTask = await Task.WhenAny(tasksToWatch);

                if (completedTask == timeoutWatchdog)
                {
                    _logger?.LogDebug($"Task '{taskKey}' iteration {iterationRuntime.IterationNumber} timeout triggered.");

                    iterationRuntime.Cancel();
                }
            }
            catch (TaskCanceledException)
            {
                // Catch TaskCanceledException so its not concidered an error, but State is changed below
            }
            catch (Exception)
            {
                // Catch-all for any errors during startup before the task even returns a Task object or after
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
                        centralExceptions.Enqueue(iterationException!);

                        _logger?.LogError($"Task '{taskKey}' iteration {iterationRuntime.IterationNumber} faulted: {iterationException!.Message}");

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

                            managedTaskRuntime.Cancel(); // Kills the lifecycle
                            break;
                        }
                    }
                    else if (workerTask.IsCanceled)
                    {
                        iterationRuntime.State = ManagedTaskState.Canceled;

                        _logger?.LogDebug($"Task '{taskKey}' iteration {iterationRuntime.IterationNumber} was cancelled.");
                    }
                    else
                    {
                        iterationRuntime.State = ManagedTaskState.Completed;

                        _logger?.LogDebug($"Task '{taskKey}' iteration {iterationRuntime.IterationNumber} completed sucessfully.");
                    }
                }
                else
                {
                    iterationRuntime.State = ManagedTaskState.Canceled;

                    // Warn if the main task is still running unexpectedly
                    // NOTE: In .NET, there is no built-in way to forcibly terminate a Task from outside. 
                    // Even though we stopped awaiting it (via force cancel or watchdog), the task may still be executing.
                    // This can happen if the task ignored cancellation requests or is stuck in a blocking operation.
                    // Logging this is crucial because it may continue consuming resources or causing unintended side effects.

                    _logger?.LogWarning($"Task '{taskKey}' iteration {iterationRuntime.IterationNumber} still running after being canceled. Are you checking the iteration token?");
                }
            }

            var upsert1 = _taskRegistryRuntime.Upsert(managedTaskRuntime, iterationRuntime, null, iterationException);
            if (!upsert1.MethodSuccess)
            {
                throw upsert1.Exception;
            }
        }

        return workerTask;
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