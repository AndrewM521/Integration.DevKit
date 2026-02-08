using AndrewM5.DevKit.TaskManagement.Abstractions;
using AndrewM5.DevKit.TaskManagement.Scheduling.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.TaskManagement.Scheduling;

public static class TaskScheduleHost
{
    private const string NotInitializedMsg = "ThreadingSchedulerHost has not been initialized.";

    private static IServiceProvider? _serviceProvider;
    private static ITaskScheduleService? _schedulerService;
    private static ITaskScheduleRegistry? _taskScheduleRegistry;

    public static void Initialize(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null)
        {
            throw new ArgumentNullException(nameof(serviceProvider));
        }

        _serviceProvider = serviceProvider;
        
        _schedulerService = _serviceProvider.GetService<ITaskScheduleService>();
        if (_schedulerService == null)
        {
            throw new InvalidOperationException($"{nameof(ITaskScheduleService)} is not registered. Make sure you call ... when configuring services before initializing {nameof(TaskScheduleHost)}.");
        }

        _taskScheduleRegistry = _serviceProvider.GetService<ITaskScheduleRegistry>();
        if (_taskScheduleRegistry == null)
        {
            throw new InvalidOperationException($"{nameof(ITaskScheduleRegistry)} is not registered. Make sure you call AddTaskManager() when configuring services before initializing {nameof(TaskScheduleHost)}.");
        }
    }

    public static IServiceProvider ServiceProvider
    {
        get
        {
            if (_serviceProvider == null)
            {
                throw new InvalidOperationException(NotInitializedMsg);
            }

            return _serviceProvider;
        }
    }

    public static ITaskScheduleService SchedulerService
    {
        get
        {
            if (_schedulerService == null)
            {
                throw new InvalidOperationException(NotInitializedMsg);
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
                throw new InvalidOperationException(NotInitializedMsg);
            }

            return _taskScheduleRegistry;
        }
    }
}
