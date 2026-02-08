using AndrewM5.DevKit.TaskManagement.Abstractions;
using AndrewM5.DevKit.TaskManagement.Scheduling.Abstractions;
using AndrewM5.DevKit.TaskManagement.Scheduling.Services;
using AndrewM5.DevKit.TaskManagement.Scheduling.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.TaskManagement.Scheduling;

public static class TaskScheduleServiceCollection
{
    public static IServiceCollection AddTaskScheduling(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        // Optionally, also register ThreadLockManager if you want it auto-resolved
        services.AddSingleton<ITaskScheduleService, TaskScheduleService>();

        services.AddSingleton<ITaskScheduleRegistry, TaskScheduleRegistry>();

        return services;
    }
}
