using AndrewM5.DevKit.TaskManagement.Abstractions.Interfaces;
using AndrewM5.DevKit.TaskManagement.Abstractions.Settings;
using AndrewM5.DevKit.ThreadLocks;
using AndrewM5.DevKit.ThreadLocks.Abstractions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.TaskManagement.Services;

public static class TaskManagementServiceCollection
{
    public static IServiceCollection AddTaskManagement(this IServiceCollection services, IConfiguration config)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }
        
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        // Bind settings
        services.Configure<TaskManagerSettings>(config.GetSection("AndrewM5.DevKit:TaskManager"));

        // Register Task Registry (holds snapshots/history of tasks)
        services.AddSingleton<IManagedTaskRegistry>(_ => new ManagedTaskRegistry(maxEntries: 5000));

        // Register TaskManager as singleton
        services.AddSingleton<ITaskManager, TaskManager>();

        // Optionally, also register ThreadLockManager if you want it auto-resolved
        services.AddSingleton<IThreadLockManager, ThreadLockManager>();

        return services;
    }

    public static IServiceCollection AddTaskScheduling(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        //services.AddSingleton<OLD_TaskScheduleExecutor>();

        //services.AddSingleton<ITaskScheduleService, OLD_TaskScheduleService>();

        //services.AddSingleton<ITaskScheduleRegistry, OLD_TaskScheduleRegistry>();

        return services;
    }
}
