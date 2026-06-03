/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using AndrewM5.DevKit.CustomLogger.Contracts.Interfaces;
using AndrewM5.DevKit.CustomLogger.Contracts.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
namespace AndrewM5.DevKit.CustomLogger;

/// <summary>
/// Provides a static entry point to access the Logging module. 
/// </summary>
/// <remarks>
/// This acts as a static wrapper for services resolved from the Dependency Injection container. 
/// It must be registered and initialized during application startup (e.g., in Program.cs or Startup.cs)
/// </remarks>
public static class Service_CustomLogger
{
    private const string NoInit = "Service_CustomLogger has not been initialized.";

    private static ILogRegistry? _logRegistry;
    private static ICustomLoggerManager? _loggerManager;

    /// <summary>
    /// Registers the custom logging infrastructure, including settings, the logger manager, and the log registry.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="config">The <see cref="IConfiguration"/> instance used to bind logging settings.</param>
    /// <returns>The modified <see cref="IServiceCollection"/> for further chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="services"/> or <paramref name="config"/> is null.</exception>
    public static IServiceCollection AddCustomLogging(this IServiceCollection services, IConfiguration config)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        // Bind LoggerManagerSettings
        services.Configure<LoggerManagerSettings>(config.GetSection("AndrewM5.DevKit:CustomLogger"));

        // Register the concrete class
        services.AddSingleton<ICustomLoggerManager, CustomLoggerManager>();

        services.AddSingleton<ILogRegistry, LogRegistry>();

        return services;
    }

    /// <summary>
    /// Initializes the static logging host with the required services from the service provider.
    /// </summary>
    /// <param name="sp">The <see cref="IServiceProvider"/> containing the registered logging services.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="sp"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <see cref="ILogRegistry"/> or <see cref="ICustomLoggerManager"/> are not registered in the DI container.
    /// </exception>
    public static void Initialize(IServiceProvider sp)
    {
        if (sp == null)
        {
            throw new ArgumentNullException(nameof(sp));
        }

        _logRegistry = sp.GetService<ILogRegistry>();
        if (_logRegistry == null)
        {
            throw new InvalidOperationException($"{nameof(ILogRegistry)} is not registered, make sure to call AddCustomLogging() when configuring services.");
        }

        _loggerManager = sp.GetService<ICustomLoggerManager>();
        if (_loggerManager == null)
        {
            throw new InvalidOperationException($"{nameof(ICustomLoggerManager)} is not registered, make sure to call AddCustomLogging() when configuring services.");
        }
    }

    /// <summary>
    /// Gets the global instance of the <see cref="ICustomLoggerManager"/>.
    /// </summary>
    /// <value>The current logger manager instance.</value>
    /// <exception cref="InvalidOperationException">Thrown if accessed before <see cref="Initialize"/> is called.</exception>
    public static ICustomLoggerManager LoggerManager
    {
        get
        {
            if (_loggerManager == null)
            {
                throw new InvalidOperationException(NoInit);
            }

            return _loggerManager;
        }
    }

    /// <summary>
    /// Gets the global instance of the <see cref="ILogRegistry"/>.
    /// </summary>
    /// <value>The current log registry instance.</value>
    /// <exception cref="InvalidOperationException">Thrown if accessed before <see cref="Initialize"/> is called.</exception>
    public static ILogRegistry LogRegistry
    {
        get
        {
            if (_logRegistry == null)
            {
                throw new InvalidOperationException(NoInit);
            }

            return _logRegistry;
        }
    }
}
