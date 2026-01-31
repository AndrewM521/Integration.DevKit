using AndrewM5.DevKit.Threading.Scheduling.Abstractions;
using AndrewM5.DevKit.Threading.Scheduling.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.Threading.Scheduling;

public static class ThreadingScheduleServiceCollection
{
    public static IServiceCollection AddScheduling(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        // Optionally, also register ThreadLockManager if you want it auto-resolved
        services.AddSingleton<ITaskSchedulerService, TaskScheduleService>();

        return services;
    }
}
