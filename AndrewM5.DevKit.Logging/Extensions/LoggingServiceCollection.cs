using AndrewM5.DevKit.Logging.Abstractions;
using AndrewM5.DevKit.Logging.Abstractions.Settings;
using AndrewM5.DevKit.Logging.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AndrewM5.DevKit.Logging.Extensions;

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
        services.Configure<LoggerManagerSettings>(config.GetSection("LoggerManagerSettings"));

        // Register the concrete class
        services.AddSingleton<ICustomLoggerManager, CustomLoggerManager>();

        return services;
    }

    public static IServiceCollection AddCustomLoggingFlushService(this IServiceCollection services, IConfiguration config)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        // Ensure LoggerManager is already registered
        if (!services.Any(sd => sd.ServiceType == typeof(ICustomLoggerManager)))
        {
            throw new InvalidOperationException($"{nameof(ICustomLoggerManager)} is not registered. You must call AddCustomLogging() before calling AddCustomLoggingFlushService().");
        }

        // Bind LogFlushServiceSettings
        services.Configure<LogFlushServiceSettings>(config.GetSection("LogFlushServiceSettings"));

        // Register the concrete class and inject ICustomLoggerManager
        services.AddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<LogFlushServiceSettings>>();
            var loggerManager = sp.GetRequiredService<ICustomLoggerManager>();

            return new LogFlushService(settings, loggerManager);
        });

        services.AddSingleton<LogFlushService>();
        services.AddSingleton<ILogFlushService>(sp => sp.GetRequiredService<LogFlushService>());

        // Register the hosted service so it runs automatically
        services.AddHostedService(sp => sp.GetRequiredService<LogFlushService>());

        return services;
    }
}
