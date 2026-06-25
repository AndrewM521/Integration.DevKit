using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Integration.DevKit.CustomLogger.Contracts;

namespace Integration.DevKit.CustomLogger.Flusher;

/// <summary>
/// Provides a static entry point to access the Logger Flusher module 
/// </summary>
/// <remarks>
/// This acts as a static wrapper for services resolved from the Dependency Injection container. 
/// It must be registered and initialized during application startup (e.g., in Program.cs or Startup.cs)
/// </remarks>
public static class Service_CustomLogFlusher
{
    private static readonly IServiceCollection _internalServiceCollection = new ServiceCollection();
    private static ILogFlusher? _logFlushService;

    private const string NoInit = "Service_LogFlusher has not been initialized.";

    /// <summary>
    /// Registers the <see cref="LogFlusher"/> as a singleton and a hosted background service.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="config">The <see cref="IConfiguration"/> instance used to bind flusher settings.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining calls.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="services"/> or <paramref name="config"/> is null.</exception>
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
        services.Configure<LogFlushServiceSettings>(config.GetSection("Integration.DevKit:CustomLoggerFlusher"));

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

    /// <summary>
    /// Initializes the static <see cref="LogFlushService"/>.
    /// </summary>
    /// <param name="sp">The <see cref="IServiceProvider"/> used to resolve logging and flushing services.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="sp"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the core Logging module is missing (requires <see cref="ICustomLoggerManager"/> and <see cref="ILogRegistry"/>) 
    /// or if the Flusher service itself has not been registered.
    /// </exception>
    public static void Initialize(IServiceProvider sp)
    {
        if (sp == null)
        {
            throw new ArgumentNullException(nameof(sp));
        }

        try
        {
            _ = sp.GetRequiredService<ICustomLoggerManager>();
            _ = sp.GetRequiredService<ILogRegistry>();
        }
        catch (Exception)
        {
            throw new InvalidOperationException($"{nameof(Service_CustomLogFlusher)} requires the Logging module. Call AddCustomLogging() before AddCustomLogFlusher()");
        }

        _logFlushService = sp.GetService<ILogFlusher>();
        if (_logFlushService == null)
        {
            throw new InvalidOperationException($"{nameof(ILogFlusher)} is not registered, make sure to call AddCustomLogFlusher() when configuring services.");
        }
    }


    /// <summary>
    /// Initializes the static <see cref="Service_CustomLogger.LoggerManager"/>, <see cref="Service_CustomLogger.LogRegistry"/>, and <see cref="LogFlushService"/>.
    /// </summary>
    /// <remarks>
    /// This should only be used if your service provider is already built as this adds to an internal service collection. 
    /// </remarks>
    public static void Initialize_OnDemand(IConfiguration configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        _internalServiceCollection.AddCustomLogging(configuration);
        _internalServiceCollection.AddCustomLogFlusher(configuration);

        var provider = _internalServiceCollection.BuildServiceProvider();

        _logFlushService = provider.GetRequiredService<ILogFlusher>();
    }

    /// <summary>
    /// Gets the global instance of the <see cref="ILogFlusher"/> service.
    /// </summary>
    /// <value>The current log flusher instance.</value>
    /// <exception cref="InvalidOperationException">Thrown if accessed before <see cref="Initialize"/> is called.</exception>
    public static ILogFlusher LogFlushService
    {
        get
        {
            if (_logFlushService == null)
            {
                throw new InvalidOperationException(NoInit);
            }

            return _logFlushService;
        }
    }
}
