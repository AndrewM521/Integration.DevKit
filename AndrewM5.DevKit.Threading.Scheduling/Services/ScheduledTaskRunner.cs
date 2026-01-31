using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.Threading.Abstractions;
using AndrewM5.DevKit.Threading.Services;
using Microsoft.Extensions.Logging;

namespace AndrewM5.DevKit.Threading.Scheduling.Services;

internal class ScheduledTaskRunner : IDisposable
{
    public Task? RunnerTask { get => _runnerTask; }

    private readonly ITaskManager _taskManager;
    private readonly IThreadLockManager _lockManager;
    private readonly ITaskRegistry _taskRegistry;
    private readonly ILogger? _logger;

    private readonly Func<ManagedTask> _taskFactory;
    private readonly TaskScheduleStrategy _strategy;
    private readonly string _taskKey;
    private readonly int _maxRunCount;

    private Task? _runnerTask;
    private CancellationTokenSource _cts = new();
    private int _currentRunCount = 0;

    public ScheduledTaskRunner(ITaskManager taskManager, IThreadLockManager lockManager, ITaskRegistry taskRegistry,
        string taskKey, Func<ManagedTask> taskFactory, TaskScheduleStrategy strategy, int maxRunCount, ILogger? logger = null)
    {
        if (taskManager == null)
        {
            throw new ArgumentNullException(nameof(taskManager));
        }
        if (lockManager == null)
        {
            throw new ArgumentNullException(nameof(lockManager));
        }
        if (taskRegistry == null)
        {
            throw new ArgumentNullException(nameof(taskRegistry));
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
        _lockManager = lockManager;
        _taskRegistry = taskRegistry;
        _logger = logger;
        _taskKey = taskKey;
        _taskFactory = taskFactory;
        _strategy = strategy;
        _maxRunCount = maxRunCount;
    }

    public OperationResult<bool> StartSchedule()
    {
        var result = new OperationResult<bool>();

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
                        var now = DateTime.UtcNow;
                        var nextRun = _strategy.GetNextTargetTime();

                        if (isFirstRun)
                        {
                            isFirstRun = false;

                            if (now < nextRun)
                            {
                                var delay = nextRun - now;
                                await Task.Delay(delay, _cts.Token);
                            }
                            else if (!_strategy.ExecImmediately)
                            {
                                nextRun = _strategy.GetNextTargetTime();

                                var delay = nextRun - DateTime.UtcNow;
                                if (delay > TimeSpan.Zero)
                                {
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

            return result.SetMethodSuccess(true);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    public OperationResult<bool> StopSchedule()
    {
        var result = new OperationResult<bool>();

        try
        {
            _cts?.Cancel();
            _runnerTask = null;

            return result.SetMethodSuccess(true);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    private async Task ExecuteRunAsync()
    {
        string lockKey = $"ScheduledTaskRunner.Lock:{_taskKey}";

        // Ensure only one execution at a time
        var lockResult = await _lockManager.TryEnterAsyncLock(lockKey);
        if (!lockResult.MethodSuccess)
        {
            _logger?.LogWarning($"Could not enter lock for task '{_taskKey}': {lockResult.Exception.Message}");
            return;
        }

        try
        {
            // Check if task is already running
            var isRunningResult = _taskManager.IsTaskRunning(_taskKey);
            if (!isRunningResult.MethodSuccess)
            {
                _logger?.LogError($"Failed to check task running status: {isRunningResult.Exception.Message}");
                return;
            }

            if (!isRunningResult.Result)
            {
                var managedTask = _taskFactory();

                var startResult = await _taskManager.StartTask(managedTask, TaskExecutionMode.Asyncronous);
                if (!startResult.MethodSuccess)
                {
                    _logger?.LogError($"Failed to start task '{_taskKey}': {startResult.Exception.Message}");
                    return;
                }

                await startResult.Result.GetTaskObject()!;

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
