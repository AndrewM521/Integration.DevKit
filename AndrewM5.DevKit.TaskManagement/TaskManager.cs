using AndrewM5.DevKit.Core;
using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.Logging.Abstractions;
using AndrewM5.DevKit.TaskManagement.Abstractions.Interfaces;
using AndrewM5.DevKit.TaskManagement.Abstractions.Models;
using AndrewM5.DevKit.TaskManagement.Abstractions.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Reflection;
using System.Threading;

namespace AndrewM5.DevKit.TaskManagement;

public class TaskManager : ITaskManager
{
    public TaskManagerSettings RuntimeSettings { get; init; }

    private readonly ConcurrentDictionary<string, ManagedTaskRuntime> _tasks = new ConcurrentDictionary<string, ManagedTaskRuntime>();
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly ICustomLogger _logger;

    private readonly SemaphoreSlim _taskLimiter;
    private readonly TaskRegistryRuntime _taskRegistryRuntime;
    
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


    public async Task<OperationResult<ITaskHandle>> StartTask(IManagedTask managedTask, TaskExecutionMode executionMode, ManagedTaskSettings? settings = null, CancellationToken cancellationToken = default)
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

            return result.SetMethodSuccess(new ManagedTaskHandle(managedTaskRuntime));
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

    public IEnumerable<string> GetAllRunningTaskKeys()
    {
        return _tasks.Keys;
    }

    public async Task AwaitAllTasksToFinish(List<Task> tasksList)
    {
        await Task.WhenAll(tasksList).ConfigureAwait(false);
    }

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
    private async Task RunManagedTaskAsync(ManagedTaskRuntime managedTaskRuntime)
    {
        var exceptions = new List<Exception>();

        try
        {
            await _taskLimiter.WaitAsync();

            managedTaskRuntime.StartTime = DateTime.UtcNow;
            managedTaskRuntime.State = ManagedTaskState.Running;

            var runtimeSettings = managedTaskRuntime.RuntimeSettings;
            Task? workerTask = null;

            var upsert = _taskRegistryRuntime.Upsert(managedTaskRuntime);
            if (!upsert.MethodSuccess)
            {
                throw upsert.Exception;
            }

            while (!managedTaskRuntime._lifecycleCTS.Token.IsCancellationRequested &&
                !managedTaskRuntime._externalCT.IsCancellationRequested)
            {
                int retryCount = 0;
                bool firstRun = true;
                bool needRetry = false;
                bool maxRetriesHit = false;

                managedTaskRuntime.ResetIterationToken();

                var iterationCTS = managedTaskRuntime._iterationCTS;

                // Create a *fresh linked token for THIS iteration*
                using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(
                    iterationCTS.Token,
                    managedTaskRuntime._lifecycleCTS.Token,
                    managedTaskRuntime._externalCT
                );

                var token = linkedCts.Token;

                // Wait until the next scheduled run
                var strategy = managedTaskRuntime.RuntimeSettings.NextRunStrategy;
                if (strategy != null)
                {
                    var target = strategy.GetNextTargetDTM(managedTaskRuntime.IterationCount);

                    _logger?.LogDebug(@$"
                        Task '{managedTaskRuntime.UserTask.TaskKey}' 
                            Next Local Target: {target}
                            Next UTC Target: {target.ToUniversalTime()}");

                    await DelayUntilNextRun(target, token);

                    strategy.LastTargetDTM = target;
                }

                managedTaskRuntime.IncrementIteration();

                while (!token.IsCancellationRequested && (firstRun || needRetry))
                {
                    firstRun = false;
                    needRetry = false;

                    workerTask = managedTaskRuntime.UserTask.DoTaskWork(token);

                    _ = workerTask.ContinueWith(t =>
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

                    Task cancelWatcher = Task.Delay(Timeout.Infinite, token);

                    Task? timeoutWatchdog = null;
                    if (managedTaskRuntime.UserTask.Timeout.HasValue)
                    {
                        timeoutWatchdog = Task.Delay(managedTaskRuntime.UserTask.Timeout.Value, token);
                    }

                    List<Task> tasksToWatch = new List<Task> { workerTask, cancelWatcher };
                    if (timeoutWatchdog != null)
                    {
                        tasksToWatch.Add(timeoutWatchdog);
                    }

                    // Wait for the first to complete
                    Task completedTask = await Task.WhenAny(tasksToWatch);

                    if (completedTask == cancelWatcher)
                    {
                        _logger?.LogDebug($"Cancel watcher triggered for '{managedTaskRuntime.UserTask.TaskKey}'.");
                    }
                    else if (completedTask == timeoutWatchdog)
                    {
                        iterationCTS.Cancel();

                        _logger?.LogDebug($"Timeout watchdog triggered for '{managedTaskRuntime.UserTask.TaskKey}'.");
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
                            break;
                        }

                        if (runtimeSettings.MaxRetryCount != -1 && retryCount >= runtimeSettings.MaxRetryCount)
                        {
                            maxRetriesHit = true;

                            _logger?.LogWarning($"Retry limit reached for '{managedTaskRuntime.UserTask.TaskKey}'.");
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

                if (maxRetriesHit && runtimeSettings.StopIterationAfterMaxRetries)
                {
                    _logger?.LogWarning($"Stopping further iterations for '{managedTaskRuntime.UserTask.TaskKey}' due to retry limit and {nameof(managedTaskRuntime.RuntimeSettings.StopIterationAfterMaxRetries)}.");
                    break; // exits outer loop
                }

                if (!ShouldRunAgain(managedTaskRuntime))
                {
                    break;
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

            await GCManager.CallGC_Collect($"Managed Task '{managedTaskRuntime.UserTask.TaskKey}' Completed");
        }
    }

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

    private bool ShouldRunAgain(ManagedTaskRuntime managedTaskRuntime)
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