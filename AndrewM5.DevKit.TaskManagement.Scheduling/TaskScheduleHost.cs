using AndrewM5.DevKit.Logging.Abstractions;
using AndrewM5.DevKit.TaskManagement.Abstractions;
using AndrewM5.DevKit.TaskManagement.Scheduling.Abstractions;
using AndrewM5.DevKit.TaskManagement.Services;
using AndrewM5.DevKit.ThreadLocks.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.TaskManagement.Scheduling;

public static class TaskScheduleHost
{
    private const string NoInit = "TaskScheduleHost has not been initialized.";

    private static ITaskScheduleService? _schedulerService;
    private static ITaskScheduleRegistry? _taskScheduleRegistry;

    public static void Initialize(IServiceProvider sp)
    {
        if (sp == null)
        {
            throw new ArgumentNullException(nameof(sp));
        }

        try
        {
            _ = sp.GetRequiredService<ITaskRegistry>();
            _ = sp.GetRequiredService<ITaskManager>();
            _ = sp.GetRequiredService<IThreadLockManager>();   
        }
        catch (Exception)
        {
            throw new InvalidOperationException($"{nameof(TaskScheduleServiceCollection)} requires the TaskManagement module. Call AddTaskManagement() before AddTaskScheduling()");
        }

        _schedulerService = sp.GetService<ITaskScheduleService>();
        if (_schedulerService == null)
        {
            throw new InvalidOperationException($"{nameof(ITaskScheduleService)} is not registered. Make sure you call AddTaskScheduling() when configuring services.");
        }

        _taskScheduleRegistry = sp.GetService<ITaskScheduleRegistry>();
        if (_taskScheduleRegistry == null)
        {
            throw new InvalidOperationException($"{nameof(ITaskScheduleRegistry)} is not registered. Make sure you call AddTaskScheduling() when configuring services.");
        }
    }

    public static ITaskScheduleService SchedulerService
    {
        get
        {
            if (_schedulerService == null)
            {
                throw new InvalidOperationException(NoInit);
            }

            return _schedulerService;
        }
    }

    public static ITaskScheduleRegistry TaskScheduleRegistry
    {
        get
        {
            if (_taskScheduleRegistry == null)
            {
                throw new InvalidOperationException(NoInit);
            }

            return _taskScheduleRegistry;
        }
    }
}
