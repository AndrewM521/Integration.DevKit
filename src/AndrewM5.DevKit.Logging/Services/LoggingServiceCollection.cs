using AndrewM5.DevKit.Logging.Abstractions.Options;
using AndrewM5.DevKit.Logging.Contracts.Interfaces;
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
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining calls.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="services"/> or <paramref name="config"/> is null.</exception>
    /// <remarks>
    /// This method binds the configuration section <c>"AndrewM5.DevKit:LoggerManager"</c> to 
    /// <see cref="LoggerManagerSettings"/> and registers both <see cref="ICustomLoggerManager"/> 
    /// and <see cref="ILogRegistry"/> as singletons.
    /// </remarks>
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
        services.Configure<LoggerManagerSettings>(config.GetSection("AndrewM5.DevKit:LoggerManager"));

        // Register the concrete class
        services.AddSingleton<ICustomLoggerManager, CustomLoggerManager>();

        services.AddSingleton<ILogRegistry, LogRegistry>();

        return services;
    }
}
