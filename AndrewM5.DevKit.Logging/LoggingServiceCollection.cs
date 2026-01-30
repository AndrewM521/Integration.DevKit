using AndrewM5.DevKit.Logging.Abstractions;
using AndrewM5.DevKit.Logging.Abstractions.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.Logging;

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

        // Register the concrete service
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

        // Bind LogFlushServiceSettings
        services.Configure<LogFlushServiceSettings>(config.GetSection("LogFlushServiceSettings"));

        // Register the concrete service
        services.AddSingleton<LogFlushService>();
        services.AddSingleton<ILogFlushService>(sp => sp.GetRequiredService<LogFlushService>());

        // Register the hosted service so it runs automatically
        services.AddHostedService(sp => sp.GetRequiredService<LogFlushService>());

        return services;
    }
}
