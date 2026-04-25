using AndrewM5.DevKit.CustomLogger;
using AndrewM5.DevKit.CustomLogger.Contracts.Interfaces;
using AndrewM5.DevKit.CustomLogger.Contracts.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.Logging.Services;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> to simplify the registration 
/// of custom logging services and their configurations.
/// </summary>
public static class LoggingServiceCollection
{
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
}
