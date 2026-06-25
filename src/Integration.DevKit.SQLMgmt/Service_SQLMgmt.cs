/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.SQLMgmt.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.DevKit.SQLMgmt;

/// <summary>
/// Provides a static entry point to access the SqlClientManagement module
/// </summary>
/// <remarks>
/// This host acts as a static wrapper for services resolved from the Dependency Injection container. 
/// It must be registered and initialized during application startup (e.g., in Program.cs or Startup.cs)
/// </remarks>
public static class Service_SQLMgmt
{
    private static readonly IServiceCollection _internalServiceCollection = new ServiceCollection();
    private const string NoInit = "Service_SQLMgmt has not been initialized.";

    private static ISQLManager? _sqlManager;

    /// <summary>
    /// Registers the <see cref="ISQLManager"/> and its concrete implementation into the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configuration">The application configuration used to bind <see cref="SQLManagerSettings"/>.</param>
    /// <returns>The original <see cref="IServiceCollection"/> instance to allow for fluent method chaining.</returns>
    public static IServiceCollection AddSQLMgmt(this IServiceCollection services, IConfiguration configuration)
    {
        // Bind settings
        services.Configure<SQLManagerSettings>(configuration.GetSection("Integration.DevKit:SQLManagement"));

        // Register the concrete class
        services.AddSingleton<ISQLManager, SQLManager>();

        return services;
    }

    /// <summary>
    /// Initializes the static <see cref="SQLManager"/>.
    /// </summary>
    /// <param name="sp">The service provider containing the registered SQL management services.</param>
    /// <exception cref="ArgumentNullException">Thrown if the provided <paramref name="sp"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <see cref="ISQLManager"/> is not registered in the service collection.
    /// </exception>
    public static void Initialize(IServiceProvider sp)
    {
        if (sp == null)
        {
            throw new ArgumentNullException(nameof(sp));
        }

        _sqlManager = sp.GetService<ISQLManager>();
        if (_sqlManager == null)
        {
            throw new InvalidOperationException($"{nameof(ISQLManager)} is not registered. Make sure you call AddSqlDBManagement() when configuring services.");
        }
    }

    /// <summary>
    /// Initializes the static <see cref="SQLManager"/>.
    /// </summary>
    /// <param name="configuration">The application configuration used to bind <see cref="SQLManagerSettings"/>.</param>
    /// <remarks>
    /// This should only be used if your service provider is already built as this adds to an internal service collection. 
    /// </remarks>
    public static void Initialize_OnDemand(IConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        _internalServiceCollection.AddSQLMgmt(configuration);

        var provider = _internalServiceCollection.BuildServiceProvider();

        _sqlManager = provider.GetRequiredService<ISQLManager>();
    }

    /// <summary>
    /// Gets the singleton instance of the <see cref="ISQLManager"/> for the current process.
    /// </summary>
    /// <value>
    /// The global <see cref="ISQLManager"/> instance used to orchestrate database clients.
    /// </value>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the host has not been initialized via <see cref="Initialize"/>.
    /// </exception>
    public static ISQLManager SQLManager
    {
        get
        {
            if (_sqlManager == null)
            {
                throw new InvalidOperationException(NoInit);
            }

            return _sqlManager;
        }
    }
}
