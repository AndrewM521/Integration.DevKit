using AndrewM5.DevKit.Logging.Abstractions;
using AndrewM5.DevKit.Logging.Abstractions.Settings;
using AndrewM5.DevKit.Logging.Flusher.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AndrewM5.DevKit.Logging.Flusher;

public static class LogFlusherServiceCollection
{
    public static IServiceCollection AddCustomLogFlusher(this IServiceCollection services, IConfiguration config)
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
        services.Configure<LogFlushServiceSettings>(config.GetSection("AndrewM5.DevKit:LogFlushService"));

        // Register the concrete class and inject ICustomLoggerManager
        services.AddSingleton(sp =>
        {
            var settings = sp.GetRequiredService<IOptions<LogFlushServiceSettings>>();
            var loggerManager = sp.GetRequiredService<ICustomLoggerManager>();
            var logRegistry = sp.GetRequiredService<ILogRegistry>();

            return new LogFlusher(settings, loggerManager, logRegistry);
        });

        services.AddSingleton<ILogFlusher>(sp => sp.GetRequiredService<LogFlusher>());

        // Register the hosted service so it runs automatically
        services.AddHostedService<LogFlusher>();

        return services;
    }
}
