using AndrewM5.DevKit.SqlManagement.Contracts.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndrewM5.DevKit.SqlManagement.Services;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> to register SQL management services.
/// </summary>
public static class SqlDBManagementServiceCollection
{
    /// <summary>
    /// Registers the <see cref="ISqlDBManager"/> and its concrete implementation into the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <returns>The updated <see cref="IServiceCollection"/> for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if the <paramref name="services"/> argument is null.</exception>
    /// <remarks>
    /// The <see cref="ISqlDBManager"/> is registered as a Singleton to ensure consistent management 
    /// of database clients and their lifecycles throughout the application.
    /// </remarks>
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
