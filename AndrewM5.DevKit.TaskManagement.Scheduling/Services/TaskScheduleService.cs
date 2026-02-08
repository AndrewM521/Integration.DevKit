using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.Logging.Abstractions;
using AndrewM5.DevKit.TaskManagement.Abstractions;
using AndrewM5.DevKit.TaskManagement.Scheduling.Abstractions;
using AndrewM5.DevKit.ThreadLocks.Abstractions;
using System.Collections.Concurrent;

namespace AndrewM5.DevKit.TaskManagement.Scheduling.Services;

internal sealed class TaskScheduleService : ITaskScheduleService
{
    private readonly ITaskManager _taskManager;
    private readonly IThreadLockManager _lockManager;
    private readonly ITaskRegistry _taskRegistry;
    private readonly ITaskScheduleRegistry _taskScheduleRegistry;
    private readonly ICustomLogger? _logger;

    private readonly ConcurrentDictionary<string, TaskScheduleRunner> _schedules = new();

    public TaskScheduleService(ITaskManager taskManager, ITaskRegistry taskRegistry, ITaskScheduleRegistry taskScheduleRegistry, 
        IThreadLockManager lockManager, ICustomLoggerManager? loggerManager)
    {
        _taskManager = taskManager;
        _lockManager = lockManager;
        _taskRegistry = taskRegistry;
        _taskScheduleRegistry = taskScheduleRegistry;

        _logger = loggerManager?.GetLogger("ThreadScheduleService");
    }

    public OperationResult<Task> ScheduleTask(string taskKey, Func<IManagedTask> taskFactory, ITaskScheduleStrategy strategy, int maxRunCount = -1)
    {
        var result = new OperationResult<Task>();

        try
        {
            if (_schedules.ContainsKey(taskKey))
            {
                throw new InvalidOperationException($"Task '{taskKey}' is already scheduled.");
            }

            var runner = new TaskScheduleRunner(_taskManager, _taskRegistry, _lockManager, _taskScheduleRegistry,
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

    public NullOperationResult CancelScheduledTask(string taskKey)
    {
        var result = new NullOperationResult();

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

    public NullOperationResult StartAllSchedules()
    {
        var result = new NullOperationResult();

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

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    public NullOperationResult StopAllSchedules()
    {
        var result = new NullOperationResult();

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

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }
}
