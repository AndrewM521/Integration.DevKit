using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.Logging.Abstractions;
using AndrewM5.DevKit.Threading.Abstractions;
using AndrewM5.DevKit.Threading.Scheduling.Abstractions;
using AndrewM5.DevKit.Threading.Services;
using System.Collections.Concurrent;

namespace AndrewM5.DevKit.Threading.Scheduling.Services;

internal sealed class TaskScheduleService : ITaskSchedulerService
{
    private readonly ITaskManager _taskManager;
    private readonly IThreadLockManager _lockManager;
    private readonly ITaskRegistry _taskRegistry;
    private readonly ICustomLogger? _logger;

    private readonly ConcurrentDictionary<string, ScheduledTaskRunner> _schedules = new();

    public TaskScheduleService(ITaskManager taskManager, IThreadLockManager lockManager, ITaskRegistry taskRegistry, ICustomLoggerManager? loggerManager)
    {
        _taskManager = taskManager;
        _lockManager = lockManager;
        _taskRegistry = taskRegistry;

        _logger = loggerManager?.GetLogger("ThreadScheduleService");
    }

    public OperationResult<Task> ScheduleTask(string taskKey, Func<ManagedTask> taskFactory, TaskScheduleStrategy strategy, int maxRunCount = -1)
    {
        var result = new OperationResult<Task>();

        try
        {
            if (_schedules.ContainsKey(taskKey))
            {
                throw new InvalidOperationException($"Task '{taskKey}' is already scheduled.");
            }

            var runner = new ScheduledTaskRunner(_taskManager, _lockManager, _taskRegistry, 
                taskKey, taskFactory, strategy, maxRunCount, _logger);

            _schedules[taskKey] = runner;

            runner.StartSchedule();

            return result.SetMethodSuccess(runner.RunnerTask!);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    public OperationResult<bool> CancelScheduledTask(string taskKey)
    {
        var result = new OperationResult<bool>();

        try
        {
            if (!_schedules.TryGetValue(taskKey, out var runner))
            {
                throw new KeyNotFoundException($"No schedule found for task '{taskKey}'");
            }

            return runner.StopSchedule();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    public OperationResult<bool> StartAllSchedules()
    {
        var result = new OperationResult<bool>();

        try
        {
            var errors = new List<Exception>();
            foreach (var runner in _schedules.Values)
            {
                var startSchedule = runner.StartSchedule();
                if (!startSchedule.MethodSuccess)
                {
                    errors.Add(result.Exception);
                }
            }

            if (errors.Count > 0)
            {
                throw new AggregateException(errors);
            }

            return result.SetMethodSuccess(true);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    public OperationResult<bool> StopAllSchedules()
    {
        var result = new OperationResult<bool>();

        try
        {
            var errors = new List<Exception>();
            foreach (var runner in _schedules.Values)
            {
                var stopSchedule = runner.StopSchedule();
                if (!stopSchedule.MethodSuccess)
                {
                    errors.Add(result.Exception);
                }
            }

            if (errors.Count > 0)
            {
                throw new AggregateException(errors);
            }

            return result.SetMethodSuccess(true);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }
}
