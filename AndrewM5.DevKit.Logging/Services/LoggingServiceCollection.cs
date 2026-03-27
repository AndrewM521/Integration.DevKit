using AndrewM5.DevKit.Logging.Abstractions.Settings;
using AndrewM5.DevKit.Logging.Contracts.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.Logging.Services;

public static class LoggingServiceCollection
{
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
