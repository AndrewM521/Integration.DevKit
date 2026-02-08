using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.TaskManagement.Abstractions;
using AndrewM5.DevKit.ThreadLocks.Abstractions;
using Microsoft.Extensions.Logging;

namespace AndrewM5.DevKit.TaskManagement.Scheduling.Services;

internal class TaskScheduleRunner : IDisposable
{
    public Task? RunnerTask { get => _runnerTask; }

    private readonly ITaskManager _taskManager;
    private readonly ITaskRegistry _taskRegistry;
    private readonly IThreadLockManager _lockManager;
    private readonly ITaskScheduleRegistry _taskScheduleRegistry;
    private readonly ILogger? _logger;
    

    private readonly Func<IManagedTask> _taskFactory;
    private readonly ITaskScheduleStrategy _strategy;
    private readonly string _taskKey;
    private readonly int _maxRunCount;

    private Task? _runnerTask;
    private CancellationTokenSource _cts = new();
    private int _currentRunCount = 0;

    public TaskScheduleRunner(ITaskManager taskManager, ITaskRegistry taskRegistry, IThreadLockManager lockManager, ITaskScheduleRegistry scheduleRegistry,
        string taskKey, Func<IManagedTask> taskFactory, ITaskScheduleStrategy strategy, int maxRunCount, ILogger? logger = null)
    {
        if (taskManager == null)
        {
            throw new ArgumentNullException(nameof(taskManager));
        }
        if (taskRegistry == null)
        {
            throw new ArgumentNullException(nameof(taskRegistry));
        }
        if (lockManager == null)
        {
            throw new ArgumentNullException(nameof(lockManager));
        }
        if (scheduleRegistry == null)
        {
            throw new ArgumentNullException(nameof(scheduleRegistry));
        }
        if (taskKey == null)
        {
            throw new ArgumentNullException(nameof(taskKey));
        }
        if (taskFactory == null)
        {
            throw new ArgumentNullException(nameof(taskFactory));
        }
        if (strategy == null)
        {
            throw new ArgumentNullException(nameof(strategy));
        }


        _taskManager = taskManager;
        _taskRegistry = taskRegistry;
        _lockManager = lockManager;
        _taskScheduleRegistry = scheduleRegistry;
        _logger = logger;
        _taskKey = taskKey;
        _taskFactory = taskFactory;
        _strategy = strategy;
        _maxRunCount = maxRunCount;
    }

    public NullOperationResult StartSchedule()
    {
        var result = new NullOperationResult();

        try
        {
            StopSchedule();

            _cts = new CancellationTokenSource();
            _currentRunCount = 0;

            _runnerTask = Task.Run(async () =>
            {
                bool isFirstRun = true;

                while (!_cts.Token.IsCancellationRequested && (_maxRunCount < 0 || _currentRunCount < _maxRunCount))
                {
                    try
                    {
                        var now = DateTime.Now;
                        var nextRun = _strategy.GetNextTargetTime();

                        if (isFirstRun)
                        {
                            isFirstRun = false;

                            if (now < nextRun)
                            {
                                var delay = nextRun - now;

                                _logger?.LogDebug($"Time till next run: {delay}");

                                await Task.Delay(delay, _cts.Token);
                            }
                            else if (!_strategy.ExecImmediately)
                            {
                                nextRun = _strategy.GetNextTargetTime();

                                var delay = nextRun - DateTime.Now;
                                if (delay > TimeSpan.Zero)
                                {
                                    _logger?.LogDebug($"Time till next run: {delay}");

                                    await Task.Delay(delay, _cts.Token);
                                }
                            }

                            if (nextRun < now)
                            {
                                _logger?.LogInformation($"Task '{_taskKey}' schedule starting late.");
                            }
                            else if (_strategy.ExecImmediately)
                            {
                                _logger?.LogInformation($"Task '{_taskKey}' executing immediately.");
                            }
                        }
                        else
                        {
                            var delay = nextRun - now;
                            if (delay > TimeSpan.Zero)
                            {
                                _logger?.LogDebug($"Time till next run: {delay}");

                                await Task.Delay(delay, _cts.Token);
                            }
                        }

                        _strategy.LastTargetTime = nextRun;

                        _cts.Token.ThrowIfCancellationRequested();

                        await ExecuteRunAsync();
                    }
                    catch (OperationCanceledException)
                    {
                        _logger?.LogDebug($"Schedule for '{_taskKey}' canceled.");
                        break;
                    }
                    catch (Exception ex)
                    {
                        _logger?.LogError($"Error in schedule loop for '{_taskKey}': {ex.Message}");
                    }
                }
            }, _cts.Token);

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    public NullOperationResult StopSchedule()
    {
        var result = new NullOperationResult();

        try
        {
            _cts?.Cancel();
            _runnerTask = null;

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    private async Task ExecuteRunAsync()
    {
        string lockKey = $"ScheduledTaskRunner.Lock:{_taskKey}";
        IManagedTaskSnapshot? managedTaskSnapshot = null;

        try
        {
            var lockResult = await _lockManager.TryEnterAsyncLock(lockKey);
            if (!lockResult.MethodSuccess)
            {
                throw lockResult.Exception;
            }

            var isRunningResult = _taskManager.IsTaskRunning(_taskKey);
            if (!isRunningResult.MethodSuccess)
            {
                throw isRunningResult.Exception;
            }

            if (!isRunningResult.Result)
            {
                var managedTask = _taskFactory();

                var startResult = await _taskManager.StartTask(managedTask, TaskExecutionMode.Asyncronous);
                if (!startResult.MethodSuccess)
                {
                    throw startResult.Exception;
                }

                await startResult.Result.RunningTask!;

                var tryGet = _taskRegistry.TryGet(managedTask.TaskKey, out managedTaskSnapshot);
                if (!tryGet.MethodSuccess)
                {
                    throw tryGet.Exception;
                }

                _currentRunCount++;
                _logger?.LogInformation($"Finished task '{_taskKey}' run {_currentRunCount} at {DateTime.UtcNow}");

                if (_maxRunCount > 0 && _currentRunCount >= _maxRunCount)
                {
                    _logger?.LogInformation($"Task '{_taskKey}' reached max run count.");
                    _cts.Cancel();
                }
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError($"Exception executing task '{_taskKey}': {ex.Message}");
        }
        finally
        {
            if (managedTaskSnapshot != null)
            {
                var upsert = _taskScheduleRegistry.Upsert(_taskKey, managedTaskSnapshot);
                if (!upsert.MethodSuccess)
                {
                    _logger?.LogError(upsert.Exception.Message);
                }
            }

            var exitLock = _lockManager.TryExitAsyncLock(lockKey);
            if (!exitLock.MethodSuccess)
            {
                _logger?.LogError($"Failed to release lock for task '{_taskKey}': {exitLock.Exception.Message}");
            }
        }
    }

    public void Dispose()
    {
        try { _cts?.Cancel(); }
        catch { }
        finally { _cts?.Dispose(); }
    }
}
