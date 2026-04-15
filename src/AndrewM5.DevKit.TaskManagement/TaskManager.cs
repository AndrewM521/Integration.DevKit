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

        _taskLimiter = new SemaphoreSlim(RuntimeSettings.MaxConcurrentTasks);   
    }

    /// <inheritdoc/>
    /// <exception cref="InvalidOperationException">Thrown if a task with the same key is already active.</exception>
    public async Task<OperationResult<ITaskHandle>> StartTask(ManagedTask managedTask, TaskExecutionMode executionMode, ManagedTaskSettings? settings = null, CancellationToken cancellationToken = default)
    {
        var result = new OperationResult<ITaskHandle>();

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

            managedTaskRuntime.UserTask.SetHandle(new ManagedTaskHandle(managedTaskRuntime));

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

            managedTaskRuntime.TaskToRun = RunManagedTaskAsync(managedTaskRuntime);
            
            if (executionMode == TaskExecutionMode.Syncronous)
            {
                await managedTaskRuntime.TaskToRun.ConfigureAwait(false);
            }

            return result.SetMethodSuccess(managedTaskRuntime.UserTask.Handle!);
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
                managedTaskRuntime._lifecycleCTS?.Cancel();

                managedTaskRuntime.State = ManagedTaskState.CancelRequested;

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
    public OperationResult<TimeSpan> GetTaskRuntime(string taskKey)
    {
        var result = new OperationResult<TimeSpan>();

        try
        {
            if (_tasks.TryGetValue(taskKey, out var liveTask))
            {
                if (liveTask.StartTime == DateTime.MinValue)
                {
                    return result.SetMethodSuccess(TimeSpan.Zero);
                }

                DateTime end = liveTask.EndTime;

                if (liveTask.EndTime == DateTime.MinValue)
                {
                    end = DateTime.UtcNow;
                }

                return result.SetMethodSuccess(end - liveTask.StartTime);
            }

            var tryGet = _taskRegistryRuntime._taskRegistry.TryGet(taskKey);
            if (!tryGet.MethodSuccess)
            {
                throw tryGet.Exception;
            }

            var snapshot = tryGet.Result;
            if (snapshot == null)
            {
                throw new ArgumentException("Could not find task.");
            }

            return result.SetMethodSuccess(snapshot.Runtime);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <inheritdoc/>
    public OperationResult<TimeSpan> GetTaskIterationRuntime(string taskKey)
    {
        var result = new OperationResult<TimeSpan>();

        try
        {
            if (_tasks.TryGetValue(taskKey, out var liveTask))
            {
                if (liveTask.StartTime == DateTime.MinValue)
                {
                    return result.SetMethodSuccess(TimeSpan.Zero);
                }

                DateTime end = liveTask.IterationEndTime;

                if (liveTask.EndTime == DateTime.MinValue)
                {
                    end = DateTime.UtcNow;
                }

                return result.SetMethodSuccess(end - liveTask.IterationStartTime);
            }

            var tryGet = _taskRegistryRuntime._taskRegistry.TryGet(taskKey);
            if (!tryGet.MethodSuccess)
            {
                throw tryGet.Exception;
            }

            var snapshot = tryGet.Result;
            if (snapshot == null)
            {
                throw new ArgumentException("Could not find task.");
            }

            return result.SetMethodSuccess(snapshot.Runtime);
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
        List<Exception> exceptions = new List<Exception>();
        Task? workerTask = null;

        try
        {
            await _taskLimiter.WaitAsync();

            managedTaskRuntime.StartTime = DateTime.UtcNow;
            managedTaskRuntime.State = ManagedTaskState.Running;

            var runtimeSettings = managedTaskRuntime.RuntimeSettings;
            var taskHandle = managedTaskRuntime.UserTask.Handle;

            var upsert = _taskRegistryRuntime.Upsert(managedTaskRuntime);
            if (!upsert.MethodSuccess)
            {
                throw upsert.Exception;
            }

            while (!managedTaskRuntime._lifecycleCTS.Token.IsCancellationRequested &&
                    !managedTaskRuntime._externalCT.IsCancellationRequested)
            {
                managedTaskRuntime.ResetIterationToken();

                var iterationCTS = managedTaskRuntime._iterationCTS;

                // Create a *fresh linked token for THIS iteration*
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    iterationCTS.Token,
                    managedTaskRuntime._lifecycleCTS.Token,
                    managedTaskRuntime._externalCT
                );

                var linkedToken = linkedCts.Token;

                // Wait for this task to be ready to run its next iteration
                await runtimeSettings.ContinueIterationStrategy.WaitForReadyAsync(taskHandle!, linkedToken, _logger);

                // Checking settings for permission to continue
                if (!ShouldContinueIterating(managedTaskRuntime))
                {
                    break;
                }

                if (managedTaskRuntime.RuntimeSettings.AllowParallelExecution)
                {

                }
                else
                {
                    List<Exception> iterationExceptions;

                    (iterationExceptions, workerTask) = await RunTaskIteration(managedTaskRuntime, iterationCTS, linkedToken);

                    exceptions.AddRange(iterationExceptions);
                }
            }

            //This delay here is so that the workerTask has time to fully finish when canceled without having to await the workerTask.
            //This prevents a false positive log message saying the task is still running even though it is not
            await Task.Delay(50);

            if (workerTask != null)
            {
                if (workerTask.IsCompleted)
                {
                    if (workerTask.IsFaulted)
                    {
                        managedTaskRuntime.State = ManagedTaskState.Faulted;
                    }
                    else if (workerTask.IsCanceled)
                    {
                        managedTaskRuntime.State = ManagedTaskState.Canceled;

                        exceptions.Add(new TaskCanceledException("Task has been canceled."));
                    }
                    else
                    {
                        managedTaskRuntime.State = ManagedTaskState.Completed;
                    }
                }
                else
                {
                    managedTaskRuntime.State = ManagedTaskState.Canceled;

                    exceptions.Add(new TaskCanceledException("Task has been canceled."));

                    // Warn if the main task is still running unexpectedly
                    // NOTE: In .NET, there is no built-in way to forcibly terminate a Task from outside. 
                    // Even though we stopped awaiting it (via force cancel or watchdog), the task may still be executing.
                    // This can happen if the task ignored cancellation requests or is stuck in a blocking operation.
                    // Logging this is crucial because it may continue consuming resources or causing unintended side effects.

                    _logger?.LogWarning($"Managed Task '{managedTaskRuntime.UserTask.TaskKey}' is still running after being canceled. Are you checking for token cancelation?");
                }
            }
        }
        catch (Exception ex)
        {
            exceptions.Add(ex);

            managedTaskRuntime.State = ManagedTaskState.Faulted;

            _logger?.LogError($"Managed Task '{managedTaskRuntime.UserTask.TaskKey}' threw an unexpected exception: {ex.Message}");
        }
        finally 
        {
            managedTaskRuntime.EndTime = DateTime.UtcNow;

            _tasks.TryRemove(managedTaskRuntime.UserTask.TaskKey, out _);

            Exception? finalEx = null;

            if (exceptions.Count > 0)
            {
                finalEx = new AggregateException(exceptions);
            }

            var upsert = _taskRegistryRuntime.Upsert(managedTaskRuntime, finalEx);
            if (!upsert.MethodSuccess)
            {
                throw upsert.Exception;
            }

            _taskLimiter.Release();
        }
    }

    private async Task<(List<Exception>, Task?)> RunTaskIteration(ManagedTaskRuntime managedTaskRuntime, CancellationTokenSource iterationCTS, CancellationToken linkedToken)
    {
        List<Exception> exceptions = new List<Exception>();
        Task? workerTask = null;

        int retryCount = 0;
        bool firstRun = true;
        bool needRetry = false;

        var runtimeSettings = managedTaskRuntime.RuntimeSettings;

        managedTaskRuntime.IncrementIteration();
        managedTaskRuntime.IterationStartTime = DateTime.UtcNow;

        while (!linkedToken.IsCancellationRequested && (firstRun || needRetry))
        {
            firstRun = false;
            needRetry = false;

            try
            {
                workerTask = managedTaskRuntime.UserTask.DoTaskWork(linkedToken);
            }
            catch (TaskCanceledException) {}
            

            _ = workerTask!.ContinueWith(t =>
            {
                if (t.IsFaulted)
                {
                    if (t.IsFaulted && t.Exception != null)
                    {
                        _logger?.LogError(t.Exception,
                            $"Background worker task faulted for '{managedTaskRuntime.UserTask.TaskKey}'.");
                    }
                }
            }, TaskContinuationOptions.ExecuteSynchronously);

            Task cancelWatcher = Task.Delay(Timeout.Infinite, linkedToken);

            Task? timeoutWatchdog = null;
            if (managedTaskRuntime.UserTask.Timeout.HasValue)
            {
                timeoutWatchdog = Task.Delay(managedTaskRuntime.UserTask.Timeout.Value, linkedToken);
            }

            List<Task> tasksToWatch = new List<Task> { workerTask!, cancelWatcher };
            if (timeoutWatchdog != null)
            {
                tasksToWatch.Add(timeoutWatchdog);
            }

            // Wait for the first to complete
            Task completedTask = await Task.WhenAny(tasksToWatch);

            managedTaskRuntime.IterationEndTime = DateTime.UtcNow;

            if (completedTask == cancelWatcher)
            {
                _logger?.LogDebug($"Cancel watcher triggered for '{managedTaskRuntime.UserTask.TaskKey}'.");
            }
            else if (completedTask == timeoutWatchdog)
            {
                _logger?.LogDebug($"Timeout watchdog triggered for '{managedTaskRuntime.UserTask.TaskKey}'.");

                iterationCTS?.Cancel();
            }

            Exception? runtimeException = null;

            // Capture worker exceptions per iteration
            if (workerTask.IsCompleted && workerTask.IsFaulted)
            {
                runtimeException = new Exception("Unknown worker exception");

                if (workerTask.Exception != null)
                {
                    runtimeException = workerTask.Exception.InnerException;
                }

                exceptions.Add(runtimeException!);

                _logger?.LogError($"Managed Task '{managedTaskRuntime.UserTask.TaskKey}' iteration '{managedTaskRuntime.IterationCount}' faulted: {runtimeException.Message}");

                if (!runtimeSettings.RetryOnException)
                {
                    if (runtimeSettings.StopIteratingOnException)
                    {
                        _logger?.LogWarning($"Stopping further iterations for '{managedTaskRuntime.UserTask.TaskKey}' due to {nameof(managedTaskRuntime.RuntimeSettings.StopIteratingOnException)}.");

                        iterationCTS?.Cancel();
                    }

                    break;
                }

                if (runtimeSettings.MaxRetryCount != -1 && retryCount >= runtimeSettings.MaxRetryCount)
                {
                    _logger?.LogWarning($"Retry limit reached for '{managedTaskRuntime.UserTask.TaskKey}'.");

                    if (runtimeSettings.StopIterationAfterMaxRetries)
                    {
                        _logger?.LogWarning($"Stopping further iterations for '{managedTaskRuntime.UserTask.TaskKey}' due to retry limit and {nameof(managedTaskRuntime.RuntimeSettings.StopIterationAfterMaxRetries)}.");

                        iterationCTS?.Cancel();
                    }
                    
                    break;
                }

                retryCount++;

                needRetry = true;

                _logger?.LogWarning($"Retrying '{managedTaskRuntime.UserTask.TaskKey}' (attempt {retryCount}).");
            }

            var upsert1 = _taskRegistryRuntime.Upsert(managedTaskRuntime, runtimeException);
            if (!upsert1.MethodSuccess)
            {
                throw upsert1.Exception;
            }
        }

        _logger?.LogDebug($"Managed Task '{managedTaskRuntime.UserTask.TaskKey}' has run {managedTaskRuntime.IterationCount} iteration(s).");

        return (exceptions, workerTask);
    }

    /// <summary>
    /// Performs an adaptive delay that sleeps in increments to allow for responsive cancellation 
    /// even during long wait periods.
    /// </summary>
    private async Task DelayUntilNextRun(DateTime target, CancellationToken token)
    {
        var utcTarget = target.ToUniversalTime();

        while (!token.IsCancellationRequested)
        {
            var utcNow = DateTimeOffset.UtcNow;

            if (utcNow >= utcTarget)
            {
                break;
            }

            var remaining = utcTarget - DateTimeOffset.UtcNow;

            TimeSpan wait;

            if (remaining <= TimeSpan.FromMinutes(5))
            {
                wait = TimeSpan.FromSeconds(1);
            }
            else if (remaining <= TimeSpan.FromMinutes(30))
            {
                wait = TimeSpan.FromMinutes(5);
            }
            else if (remaining <= TimeSpan.FromHours(1))
            {
                wait = TimeSpan.FromMinutes(20);
            }
            else
            {
                // For very long intervals, sleep up to 1 hour
                wait = TimeSpan.FromHours(1);
            }

            // Don't oversleep past the target
            if (wait > remaining)
            {
                wait = remaining;
            }

            await Task.Delay(wait, token);
        }
    }

    /// <summary>
    /// Evaluates if a task should proceed to its next scheduled iteration.
    /// </summary>
    private bool ShouldContinueIterating(ManagedTaskRuntime managedTaskRuntime)
    {
        var runtimeSettings = managedTaskRuntime.RuntimeSettings;

        if (managedTaskRuntime._lifecycleCTS.IsCancellationRequested || managedTaskRuntime._iterationCTS.IsCancellationRequested)
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