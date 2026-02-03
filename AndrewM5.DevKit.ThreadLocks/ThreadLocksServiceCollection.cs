using AndrewM5.DevKit.ThreadLocks.Abstractions;
using AndrewM5.DevKit.ThreadLocks.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.ThreadLocks;

public static class ThreadLocksServiceCollection
{
    public static IServiceCollection AddThreadLocks(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        // Optionally, also register ThreadLockManager if you want it auto-resolved
        services.AddSingleton<IThreadLockManager, ThreadLockManager>();

        return services;
    }
}
