using AndrewM5.DevKit.ThreadLocks.Contracts.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.ThreadLocks.Services;

/// <summary>
/// Provides extension methods for registering thread locking services into the <see cref="IServiceCollection"/>.
/// </summary>
public static class ThreadLocksServiceCollection
{
    /// <summary>
    /// Adds the <see cref="IThreadLockManager"/> to the service collection as a singleton.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for method chaining.</returns>
    /// <remarks>
    /// The manager is registered as a singleton to ensure that lock dictionaries and reference 
    /// counts are shared across the entire application lifetime.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="services"/> is null.</exception>
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
