using AndrewM5.DevKit.Logging.Abstractions.Options;
using AndrewM5.DevKit.Logging.Contracts.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AndrewM5.DevKit.Logging.Flusher.Services;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> to register and 
/// configure the background log flushing service.
/// </summary>
public static class LogFlusherServiceCollection
{
    /// <summary>
    /// Registers the <see cref="LogFlusher"/> as a singleton and a hosted background service.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="config">The <see cref="IConfiguration"/> instance used to bind flusher settings.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining calls.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="services"/> or <paramref name="config"/> is null.</exception>
    /// <remarks>
    /// This method performs the following actions:
    /// <list type="bullet">
    /// <item>Binds the <c>"AndrewM5.DevKit:LogFlushService"</c> configuration section.</item>
    /// <item>Registers <see cref="LogFlusher"/> as a singleton implementation.</item>
    /// <item>Maps <see cref="ILogFlusher"/> to the same <see cref="LogFlusher"/> singleton.</item>
    /// <item>Registers <see cref="LogFlusher"/> as an <see cref="IHostedService"/> to enable background execution.</item>
    /// </list>
    /// </remarks>
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
