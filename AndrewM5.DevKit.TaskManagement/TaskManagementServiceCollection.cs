using AndrewM5.DevKit.TaskManagement.Abstractions;
using AndrewM5.DevKit.TaskManagement.Abstractions.Settings;
using AndrewM5.DevKit.TaskManagement.Services;
using AndrewM5.DevKit.TaskManagement.Utilities;
using AndrewM5.DevKit.ThreadLocks.Abstractions;
using AndrewM5.DevKit.ThreadLocks.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.TaskManagement;

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
        services.AddSingleton<ITaskRegistry>(_ => new TaskRegistry(maxEntries: 5000));

        // Register TaskManager as singleton
        services.AddSingleton<ITaskManager, TaskManager>();

        // Optionally, also register ThreadLockManager if you want it auto-resolved
        services.AddSingleton<IThreadLockManager, ThreadLockManager>();

        return services;
    }
}
