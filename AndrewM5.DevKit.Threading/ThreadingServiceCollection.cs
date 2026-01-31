using AndrewM5.DevKit.Threading.Abstractions;
using AndrewM5.DevKit.Threading.Abstractions.Settings;
using AndrewM5.DevKit.Threading.Services;
using AndrewM5.DevKit.Threading.Utilities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.Threading;

public static class ThreadingServiceCollection
{
    public static IServiceCollection AddThreading(this IServiceCollection services, IConfiguration config)
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
        services.Configure<TaskManagerSettings>(config.GetSection("TaskManagerSettings"));

        // Register Task Registry (holds snapshots/history of tasks)
        services.AddSingleton<ITaskRegistry>(_ => new TaskRegistry(maxEntries: 5000));

        // Register TaskManager as singleton
        services.AddSingleton<ITaskManager, TaskManager>();

        // Optionally, also register ThreadLockManager if you want it auto-resolved
        services.AddSingleton<IThreadLockManager, ThreadLockManager>();

        return services;
    }
}
