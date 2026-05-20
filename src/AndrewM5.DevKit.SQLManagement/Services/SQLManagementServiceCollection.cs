/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using AndrewM5.DevKit.SQLManagement.Contracts.Interfaces;
using AndrewM5.DevKit.SQLManagement.Contracts.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.SQLManagement.Services;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> to facilitate the registration 
/// of SQL management services within the .NET Dependency Injection container.
/// </summary>
public static class SQLManagementServiceCollection
{
    /// <summary>
    /// Registers the <see cref="ISQLManager"/> and its concrete implementation into the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configuration">The application configuration used to bind <see cref="SQLManagerSettings"/>.</param>
    /// <returns>The original <see cref="IServiceCollection"/> instance to allow for fluent method chaining.</returns>
    public static IServiceCollection AddSQLManagement(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind settings
        services.Configure<SQLManagerSettings>(configuration.GetSection("AndrewM5.DevKit:SQLManagement"));

        // Register the concrete class
        services.AddSingleton<ISQLManager, SQLManager>();

        return services;
    }
}
