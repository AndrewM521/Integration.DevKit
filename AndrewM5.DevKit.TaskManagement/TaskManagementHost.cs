using AndrewM5.DevKit.TaskManagement.Abstractions;
using AndrewM5.DevKit.TaskManagement.Scheduling.Abstractions;
using AndrewM5.DevKit.ThreadLocks.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.TaskManagement;

public static class TaskManagementHost
{
    private const string NoInit = "TaskManagementHost has not been initialized.";

    private static ITaskManager? _taskManager;
    private static IThreadLockManager? _threadLockManager;
    private static ITaskRegistry? _taskRegistry;
    private static ITaskScheduleService? _schedulerService;
    private static ITaskScheduleRegistry? _taskScheduleRegistry;

    public static void InitializeTaskManagement(IServiceProvider sp)
    {
        if (sp == null)
        {
            throw new ArgumentNullException(nameof(sp));
        }

        _taskRegistry = sp.GetService<ITaskRegistry>();
        if (_taskRegistry == null)
        {
            throw new InvalidOperationException($"{nameof(ITaskRegistry)} is not registered. Make sure you call AddTaskManager() when configuring services.");
        }

        _taskManager = sp.GetService<ITaskManager>();
        if (_taskManager == null)
        {
            throw new InvalidOperationException($"{nameof(ITaskManager)} is not registered. Make sure you call AddTaskManager() when configuring services.");
        }

        _threadLockManager = sp.GetService<IThreadLockManager>();
        if (_threadLockManager == null)
        {
            throw new InvalidOperationException($"{nameof(IThreadLockManager)} is not registered. Make sure you call AddTaskManager() when configuring services.");
        }
    }

    public static void InitializeTaskScheduling(IServiceProvider sp)
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
            throw new InvalidOperationException($"Task Scheduling requires calling AddTaskManagement() before AddTaskScheduling()");
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

    public static ITaskManager TaskManager
    {
        get
        {
            if (_taskManager == null)
            {
                throw new InvalidOperationException(NoInit);
            }

            return _taskManager;
        }
    }

    public static IThreadLockManager ThreadLockManager
    {
        get
        {
            if (_threadLockManager == null)
            {
                throw new InvalidOperationException(NoInit);
            }

            return _threadLockManager;
        }
    }

    public static ITaskRegistry TaskRegistry
    {
        get
        {
            if (_taskRegistry == null)
            {
                throw new InvalidOperationException(NoInit);
            }

            return _taskRegistry;
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
