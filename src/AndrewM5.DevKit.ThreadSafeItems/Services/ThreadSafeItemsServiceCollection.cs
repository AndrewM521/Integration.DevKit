using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.ThreadSafeItems.Services;

/// <summary>
/// Provides extension methods for registering thread-safe utility services into the <see cref="IServiceCollection"/>.
/// </summary>
public static class ThreadSafeItemsServiceCollection
{
    /// <summary>
    /// Adds the <see cref="ThreadSafeFileIO"/> service to the service collection as a singleton.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the service to.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="services"/> is null.</exception>
    public static IServiceCollection AddThreadSafeItems(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.AddSingleton<ThreadSafeFileIO>();

        return services;
    }
}
