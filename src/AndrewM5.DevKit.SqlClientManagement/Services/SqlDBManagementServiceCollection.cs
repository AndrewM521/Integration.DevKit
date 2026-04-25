using AndrewM5.DevKit.SqlManagement.Contracts.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.SqlManagement.Services;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> to facilitate the registration 
/// of SQL management services within the .NET Dependency Injection container.
/// </summary>
public static class SqlDBManagementServiceCollection
{
    /// <summary>
    /// Registers the <see cref="ISqlDBManager"/> and its concrete implementation into the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The original <see cref="IServiceCollection"/> instance to allow for fluent method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="services"/> argument is <see langword="null"/>.</exception>
    public static IServiceCollection AddSqlDBManagement(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        // Register the concrete class
        services.AddSingleton<ISqlDBManager, SqlDBManager>();

        return services;
    }
}
