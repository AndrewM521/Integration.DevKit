using AndrewM5.DevKit.Core;
using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.Logging.Abstractions;
using AndrewM5.DevKit.TaskManagement.Abstractions;
using AndrewM5.DevKit.TaskManagement.Abstractions.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Reflection;

namespace AndrewM5.DevKit.TaskManagement.Services;

public class TaskManager : ITaskManager
{
    public TaskManagerSettings RuntimeSettings { get; init; }

    private readonly ConcurrentDictionary<string, ManagedTaskRuntime> _tasks = new ConcurrentDictionary<string, ManagedTaskRuntime>();
    private readonly IHostApplicationLifetime _appLifetime;
    private readonly ICustomLogger _logger;
    private readonly ITaskRegistry _taskRegistry;

    private readonly SemaphoreSlim _taskLimiter;
    
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
                _logger?.LogInformation($"[{nameof(TaskManager)}] All tasks stopped gracefully during host shutdown.");
            }
            else
            {
                _logger?.LogError($"[{nameof(TaskManager)}] {cancelResult.Exception.Message}");
            }
        });

        _logger = loggerManager.GetLogger("TaskManager");
        _taskRegistry = taskRegistry;

        RuntimeSettings = settings.Value.Clone();

        if (RuntimeSettings.MaxConcurrentTasks < 0)
        {
            RuntimeSettings.MaxConcurrentTasks = int.MaxValue;
        }

        _taskLimiter = new SemaphoreSlim(RuntimeSettings.MaxConcurrentTasks);   
    }

    public async Task<OperationResult<ITaskHandle>> StartTask(IManagedTask managedTask, TaskExecutionMode mode)
    {
        var result = new OperationResult<ITaskHandle>();
        
        ManagedTaskRuntime? managedTaskRuntime = null;

        try
        {
            if (managedTask == null)
            {
                throw new ArgumentNullException(nameof(managedTask));
            }

            managedTaskRuntime = new ManagedTaskRuntime(managedTask, mode);

            managedTaskRuntime.State = ManagedTaskState.Starting;
            
            var upsert = _taskRegistry.Upsert(ManagedTaskSnapshot.From(
                managedTaskRuntime.UserTask.TaskKey,
                managedTaskRuntime.State,
                managedTaskRuntime.StartTime,
                managedTaskRuntime.EndTime)
            );
            if (!upsert.MethodSuccess)
            {
                throw upsert.Exception;
            }

            await CreateTask(managedTaskRuntime).ConfigureAwait(false);

            return result.SetMethodSuccess(new TaskHandle(managedTaskRuntime));
        }
        catch (Exception ex)
        {
            if (managedTaskRuntime != null)
            {
                _tasks.TryRemove(managedTask.TaskKey, out _);

                managedTaskRuntime.State = ManagedTaskState.Faulted;

                var upsert = _taskRegistry.Upsert(ManagedTaskSnapshot.From(
                    managedTaskRuntime.UserTask.TaskKey,
                    managedTaskRuntime.State,
                    managedTaskRuntime.StartTime,
                    managedTaskRuntime.EndTime,
                    ex)
                );
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
                managedTaskRuntime.ForceCancelRequested = forceCancel;

                managedTaskRuntime._cancellationTokenSource?.Cancel();

                managedTaskRuntime.State = ManagedTaskState.CancelRequested;
                var upsert = _taskRegistry.Upsert(ManagedTaskSnapshot.From(
                    managedTaskRuntime.UserTask.TaskKey,
                    managedTaskRuntime.State,
                    managedTaskRuntime.StartTime,
                    managedTaskRuntime.EndTime)
                );
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

    public async Task<NullOperationResult> RestartTask(string taskKey)
    {
        var result = new NullOperationResult();

        try
        {
            if (!_tasks.TryGetValue(taskKey, out var managedTaskRuntime))
            {
                throw new KeyNotFoundException($"Task '{taskKey}' could not be found.");
            }

            var cancelTask = CancelTask(managedTaskRuntime.UserTask.TaskKey, true);
            if (!cancelTask.MethodSuccess)
            {
                throw cancelTask.Exception;
            }

            await CreateTask(managedTaskRuntime);

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
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

            var tryGet = _taskRegistry.TryGet(taskKey, out var snapshot);
            if (!tryGet.MethodSuccess)
            {
                throw tryGet.Exception;
            }

            if (!tryGet.Result)
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
    private async Task CreateTask(ManagedTaskRuntime managedTaskRuntime)
    {
        // Add task to dictionary so manager knows it's running
        if (!_tasks.TryAdd(managedTaskRuntime.UserTask.TaskKey, managedTaskRuntime))
        {
            throw new InvalidOperationException($"Task '{managedTaskRuntime.UserTask.TaskKey}' is already running.");
        }

        managedTaskRuntime._cancellationTokenSource?.Dispose();
        managedTaskRuntime._cancellationTokenSource = new CancellationTokenSource();

        managedTaskRuntime._completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        // Enforce concurrency limit before launching the task
        await _taskLimiter.WaitAsync().ConfigureAwait(false);

        if (managedTaskRuntime.ExecutionMode == TaskExecutionMode.Asyncronous)
        {
            Func<Task> taskToRun = async () =>
            {   
                Task mainTask = MainTaskMethod(managedTaskRuntime);

                Task cancelWatcher = Task.Run((Func<Task?>)(async () =>
                {
                    while (!mainTask.IsCompleted)
                    {
                        try
                        {
                            await Task.Delay(Timeout.Infinite, managedTaskRuntime._cancellationTokenSource.Token);
                        }
                        catch (TaskCanceledException)
                        {
                            // If force cancel was requested, exit the loop
                            if (managedTaskRuntime.ForceCancelRequested && !mainTask.IsCompleted)
                            {
                                _logger?.LogInformation($"Force cancel requested for task '{managedTaskRuntime.UserTask.TaskKey}'");
                                break;
                            }
                        }
                    }
                }));
                
                Task? timeoutWatchdog = null;
                if (managedTaskRuntime.UserTask.Timeout.HasValue)
                {
                    timeoutWatchdog = Task.Run((Func<Task?>)(async () =>
                    {
                        var delayTask = Task.Delay(managedTaskRuntime.UserTask.Timeout.Value);
                        var finishedTask = await Task.WhenAny(delayTask, managedTaskRuntime._completionSource.Task);
                        
                        if (finishedTask == delayTask && !mainTask.IsCompleted)
                        {
                            managedTaskRuntime.ForceCancelRequested = true;
                            managedTaskRuntime._cancellationTokenSource?.Cancel();
                        }
                    }));
                }

                List<Task> tasksToWatch = new List<Task> { mainTask, cancelWatcher };
                if (timeoutWatchdog != null)
                {
                    tasksToWatch.Add(timeoutWatchdog);
                }

                var completedTask = await Task.WhenAny(tasksToWatch);

                if (completedTask == mainTask)
                {
                    _logger?.LogInformation($"Main task '{managedTaskRuntime.UserTask.TaskKey}' finished.");
                }
                else if (completedTask == cancelWatcher)
                {
                    _logger?.LogInformation($"Cancel watcher triggered for '{managedTaskRuntime.UserTask.TaskKey}'.");
                }
                else if (timeoutWatchdog != null && completedTask == timeoutWatchdog)
                {
                    _logger?.LogInformation($"Timeout watchdog triggered for '{managedTaskRuntime.UserTask.TaskKey}'.");
                }

                // Warn if the main task is still running unexpectedly
                // NOTE: In .NET, there is no built-in way to forcibly terminate a Task from outside. 
                // Even though we stopped awaiting it (via force cancel or watchdog), the task may still be executing.
                // This can happen if the task ignored cancellation requests or is stuck in a blocking operation.
                // Logging this is crucial because it may continue consuming resources or causing unintended side effects.
                if (!mainTask.IsCompleted)
                {
                    _logger?.LogWarning($"Main task for '{managedTaskRuntime.UserTask.TaskKey}' is still running (stuck or ignored cancellation).");
                }
            };

            if (!managedTaskRuntime.IsLongRunningTask)
            {
                managedTaskRuntime.TaskToRun = Task.Run(taskToRun);
            }
            else
            {
                managedTaskRuntime.TaskToRun = Task.Factory.StartNew(taskToRun, managedTaskRuntime._cancellationTokenSource.Token, 
                    TaskCreationOptions.LongRunning, TaskScheduler.Default
                ).Unwrap();
            }
        }
        else
        {
            // sync execution blocks caller
            managedTaskRuntime.TaskToRun = MainTaskMethod(managedTaskRuntime);
            managedTaskRuntime.TaskToRun.GetAwaiter().GetResult();
        }
    }

    private async Task MainTaskMethod(ManagedTaskRuntime managedTaskRuntime)
    {
        Exception? capturedEx = null;

        try
        {
            managedTaskRuntime.StartTime = DateTime.UtcNow;
            managedTaskRuntime.State = ManagedTaskState.Running;

            await managedTaskRuntime.UserTask.DoTaskWork(managedTaskRuntime._cancellationTokenSource!.Token).ConfigureAwait(false);

            managedTaskRuntime.State = ManagedTaskState.Completed;
        }
        catch (OperationCanceledException ex)
        {
            capturedEx = ex;

            managedTaskRuntime.State = ManagedTaskState.Canceled;
            _logger?.LogInformation($"Task '{managedTaskRuntime.UserTask.TaskKey}' cancelled.");
        }
        catch (Exception ex)
        {
            capturedEx = ex;

            managedTaskRuntime.State = ManagedTaskState.Faulted;
            _logger?.LogError($"Task '{managedTaskRuntime.UserTask.TaskKey}' faulted unexpectedly. {ex.Message}");
        }
        finally
        {
            managedTaskRuntime.EndTime = DateTime.UtcNow;

            _tasks.TryRemove(managedTaskRuntime.UserTask.TaskKey, out _);
            var upsert = _taskRegistry.Upsert(ManagedTaskSnapshot.From(
                managedTaskRuntime.UserTask.TaskKey, 
                managedTaskRuntime.State, 
                managedTaskRuntime.StartTime, 
                managedTaskRuntime.EndTime,
                capturedEx)
            );
            if (!upsert.MethodSuccess)
            {
                throw upsert.Exception;
            }

            managedTaskRuntime._cancellationTokenSource?.Cancel();
            managedTaskRuntime._completionSource.TrySetResult(true);

            _taskLimiter.Release();

            await GCManager.CallGC_Collect($"Task '{managedTaskRuntime.UserTask.TaskKey}' Complete");
        }
    }
    #endregion
}