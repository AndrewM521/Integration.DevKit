using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.Logging.Abstractions;
using AndrewM5.DevKit.Threading.Abstractions;
using AndrewM5.DevKit.Threading.Abstractions.Settings;
using AndrewM5.DevKit.Threading.Utilities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Reflection;
using System.Text.RegularExpressions;

namespace AndrewM5.DevKit.Threading.Services;

public class TaskManager : ITaskManager
{
    public TaskManagerSettings RuntimeSettings { get; init; }

    private readonly ConcurrentDictionary<string, ManagedTask> _tasks = new ConcurrentDictionary<string, ManagedTask>();
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly IThreadLockManager _threadLockManager;
    private readonly ICustomLogger _logger;
    private readonly ITaskRegistry _taskRegistry;

    private readonly SemaphoreSlim _taskLimiter;
    
    public TaskManager(
        IHostApplicationLifetime appLifetime, 
        IThreadLockManager threadLockManager,
        ICustomLoggerManager loggerManager,
        ITaskRegistry taskRegistry,
        IOptions<TaskManagerSettings> settings)
    {
        if (appLifetime == null)
        {
            throw new ArgumentNullException(nameof(appLifetime));
        }

        if (threadLockManager == null)
        {
            throw new ArgumentNullException(nameof(threadLockManager));
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
            var cancelResult = CancelAllTasks();
            if (cancelResult.MethodSuccess)
            {
                _logger?.LogInformation($"[{nameof(TaskManager)}] All tasks stopped gracefully during host shutdown.");
            }
            else
            {
                _logger?.LogError($"[{nameof(TaskManager)}] {cancelResult.Exception.Message}");
            }
        });

        _threadLockManager = threadLockManager;
        _logger = loggerManager.GetLogger("TaskManager");
        _taskRegistry = taskRegistry;

        RuntimeSettings = settings.Value.Clone();

        if (RuntimeSettings.MaxConcurrentTasks < 0)
        {
            RuntimeSettings.MaxConcurrentTasks = int.MaxValue;
        }

        _taskLimiter = new SemaphoreSlim(RuntimeSettings.MaxConcurrentTasks);   
    }

    #region Task Operations
    public async Task<OperationResult<ManagedTask>> StartTask(ManagedTask managedTask, TaskExecutionMode mode)
    {
        var result = new OperationResult<ManagedTask>();

        try
        {
            if (managedTask == null)
            {
                throw new ArgumentNullException(nameof(managedTask));
            }

            if (string.IsNullOrWhiteSpace(managedTask.TaskKey))
            {
                throw new InvalidOperationException("ManagedTaks TaskKey was invalid.");
            }

            managedTask.AttachServices(this, _threadLockManager, _logger);

            managedTask.State = ManagedTaskState.Starting;
            _taskRegistry.Upsert(ManagedTaskSnapshot.From(managedTask));

            await CreateTask(managedTask, mode).ConfigureAwait(false);

            return result.SetMethodSuccess(managedTask);
        }
        catch (Exception ex)
        {
            if (managedTask != null)
            {
                _tasks.TryRemove(managedTask.TaskKey, out _);

                managedTask.State = ManagedTaskState.Faulted;
                _taskRegistry.Upsert(ManagedTaskSnapshot.From(managedTask, ex));
            }

            return result.SetMethodFailure(ex);
        }
    }

    public OperationResult<bool> CancelTask(string taskKey, bool forceCancel = false)
    {
        var result = new OperationResult<bool>();

        try
        {
            if (string.IsNullOrWhiteSpace(taskKey))
            {
                throw new ArgumentException("Task key cannot be null or whitespace.");
            }

            if (_tasks.TryGetValue(taskKey, out var managedTask))
            {
                managedTask._forceCancelRequested = forceCancel;
                managedTask._cancellationTokenSource?.Cancel();

                managedTask.State = ManagedTaskState.CancelRequested;
                _taskRegistry.Upsert(ManagedTaskSnapshot.From(managedTask));
            }

            return result.SetMethodSuccess(true);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    public OperationResult<bool> CancelAllTasks(bool forceCancel = false)
    {
        var result = new OperationResult<bool>();
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

        return result.SetMethodSuccess(true);
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
                if (liveTask._taskStartTime == DateTime.MinValue)
                {
                    return result.SetMethodSuccess(TimeSpan.Zero);
                }

                DateTime end = liveTask._taskEndTime;

                if (liveTask._taskEndTime == DateTime.MinValue)
                {
                    end = DateTime.UtcNow;
                }

                return result.SetMethodSuccess(end - liveTask._taskStartTime);
            }

            if (_taskRegistry.TryGet(taskKey, out var snapshot))
            {
                return result.SetMethodSuccess(snapshot.Runtime);
            }

            throw new ArgumentException("Could not find task.");
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
    #endregion

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

    public async Task<OperationResult<bool>> RestartTask(ManagedTask managedTask)
    {
        var result = new OperationResult<bool>();

        try
        {
            var cancelTask = CancelTask(managedTask._taskKey, true);
            if (!cancelTask.MethodSuccess)
            {
                throw cancelTask.Exception;
            }

            await CreateTask(managedTask, managedTask._executionMode);

            return result.SetMethodSuccess(true);
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Failed to restart task '{managedTask.TaskKey}': {ex.Message}");

            return result.SetMethodFailure(ex);
        }
    }
    


    #region Helpers
    private async Task CreateTask(ManagedTask managedTask, TaskExecutionMode mode)
    {
        // Add task to dictionary so manager knows it's running
        if (!_tasks.TryAdd(managedTask.TaskKey, managedTask))
        {
            throw new InvalidOperationException($"Task '{managedTask.TaskKey}' is already running.");
        }

        managedTask._cancellationTokenSource?.Dispose();
        managedTask._cancellationTokenSource = new CancellationTokenSource();

        managedTask._taskCompletionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Enforce concurrency limit before launching the task
        await _taskLimiter.WaitAsync().ConfigureAwait(false);

        managedTask._executionMode = mode;

        if (mode == TaskExecutionMode.Asyncronous)
        {
            Func<Task> taskToRun = async () =>
            {   
                Task mainTask = MainTaskMethod(managedTask);

                Task cancelWatcher = Task.Run(async () =>
                {
                    while (!mainTask.IsCompleted)
                    {
                        try
                        {
                            await Task.Delay(Timeout.Infinite, managedTask._cancellationTokenSource.Token);
                        }
                        catch (TaskCanceledException)
                        {
                            // If force cancel was requested, exit the loop
                            if (managedTask._forceCancelRequested && !mainTask.IsCompleted)
                            {
                                _logger?.LogInformation($"Force cancel requested for task '{managedTask._taskKey}'");
                                break;
                            }
                        }
                    }
                });
                
                Task? timeoutWatchdog = null;
                if (managedTask.Timeout.HasValue)
                {
                    timeoutWatchdog = Task.Run(async () =>
                    {
                        var delayTask = Task.Delay(managedTask.Timeout.Value);
                        var finishedTask = await Task.WhenAny(delayTask, managedTask._taskCompletionSource.Task);
                        
                        if (finishedTask == delayTask && !mainTask.IsCompleted)
                        {
                            managedTask._forceCancelRequested = true;
                            managedTask._cancellationTokenSource?.Cancel();
                        }
                    });
                }

                List<Task> tasksToWatch = new List<Task> { mainTask, cancelWatcher };
                if (timeoutWatchdog != null)
                {
                    tasksToWatch.Add(timeoutWatchdog);
                }

                var completedTask = await Task.WhenAny(tasksToWatch);

                if (completedTask == mainTask)
                {
                    _logger?.LogInformation($"Main task '{managedTask._taskKey}' finished.");
                }
                else if (completedTask == cancelWatcher)
                {
                    _logger?.LogInformation($"Cancel watcher triggered for '{managedTask._taskKey}'.");
                }
                else if (timeoutWatchdog != null && completedTask == timeoutWatchdog)
                {
                    _logger?.LogInformation($"Timeout watchdog triggered for '{managedTask._taskKey}'.");
                }

                // Warn if the main task is still running unexpectedly
                // NOTE: In .NET, there is no built-in way to forcibly terminate a Task from outside. 
                // Even though we stopped awaiting it (via force cancel or watchdog), the task may still be executing.
                // This can happen if the task ignored cancellation requests or is stuck in a blocking operation.
                // Logging this is crucial because it may continue consuming resources or causing unintended side effects.
                if (!mainTask.IsCompleted)
                {
                    _logger?.LogWarning($"Main task for '{managedTask.TaskKey}' is still running (stuck or ignored cancellation).");
                }
            };

            if (!managedTask._isLongRunningTask)
            {
                managedTask.TaskToRun = Task.Run(taskToRun);
            }
            else
            {
                managedTask.TaskToRun = Task.Factory.StartNew(taskToRun, managedTask._cancellationTokenSource.Token, 
                    TaskCreationOptions.LongRunning, TaskScheduler.Default
                ).Unwrap();
            }
        }
        else
        {
            // sync execution blocks caller
            managedTask.TaskToRun = MainTaskMethod(managedTask);
            managedTask.TaskToRun.GetAwaiter().GetResult();
        }
    }

    private async Task MainTaskMethod(ManagedTask managedTask)
    {
        Exception? capturedEx = null;

        try
        {
            managedTask._taskStartTime = DateTime.UtcNow;
            managedTask.State = ManagedTaskState.Running;

            await managedTask.DoTaskWork(managedTask._cancellationTokenSource!.Token).ConfigureAwait(false);

            managedTask.State = ManagedTaskState.Completed;
        }
        catch (OperationCanceledException ex)
        {
            capturedEx = ex;

            managedTask.State = ManagedTaskState.Canceled;
            managedTask.Logger?.LogInformation($"Task '{managedTask._taskKey}' cancelled.");
        }
        catch (Exception ex)
        {
            capturedEx = ex;

            managedTask.State = ManagedTaskState.Faulted;
            managedTask.Logger?.LogError($"Task '{managedTask._taskKey}' faulted unexpectedly. {ex.Message}");
        }
        finally
        {
            managedTask._taskEndTime = DateTime.UtcNow;

            _tasks.TryRemove(managedTask.TaskKey, out _);
            _taskRegistry.Upsert(ManagedTaskSnapshot.From(managedTask, capturedEx));

            managedTask._cancellationTokenSource?.Cancel();
            managedTask._taskCompletionSource.TrySetResult(true);

            _taskLimiter.Release();

            Console.WriteLine("TODO: Add CallGC_Collect");
            //GCManager.CallGC_Collect($"Task '{managedTask._taskKey}' Complete");
        }
    }
    #endregion
}

public enum TaskExecutionMode
{
    Asyncronous,
    Syncronous
}