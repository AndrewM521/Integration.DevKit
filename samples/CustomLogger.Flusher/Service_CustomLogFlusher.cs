using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace CustomLogger.Flusher;

/// <summary>
/// Provides a static entry point to access the Logger Flusher module 
/// </summary>
/// <remarks>
/// This acts as a static wrapper for services resolved from the Dependency Injection container. 
/// It must be registered and initialized during application startup (e.g., in Program.cs or Startup.cs)
/// </remarks>
public static class Service_CustomLogFlusher
{
    private static LogFlusher? _logFlushService;

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
        services.Configure<LogFlushServiceSettings>(config.GetSection("CustomLoggerFlusher"));

        // Register the concrete implementation as a regular Singleton first.
        // .NET will automatically infer and inject its constructor dependencies here.
        services.TryAddSingleton<LogFlusher>();

        // Forward the Hosted Service to the concrete Singleton instance
        services.TryAddTransient<IHostedService>(sp => sp.GetRequiredService<LogFlusher>());

        return services;
    }

    /// <summary>
    /// Initializes the static <see cref="LogFlushService"/>.
    /// </summary>
    /// <param name="sp">The <see cref="IServiceProvider"/> used to resolve logging and flushing services.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="sp"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the core Logging module is missing (requires <see cref="ICustomLoggerManager"/> and <see cref="ILogFileRegistry"/>) 
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
            _ = sp.GetRequiredService<CustomLoggerManager>();
            _ = sp.GetRequiredService<LogFileRegistry>();
        }
        catch (Exception)
        {
            throw new InvalidOperationException($"{nameof(Service_CustomLogFlusher)} requires the Logging module. Call AddCustomLogging() before AddCustomLogFlusher()");
        }

        _logFlushService = sp.GetService<LogFlusher>();
        if (_logFlushService == null)
        {
            throw new InvalidOperationException($"{nameof(LogFlusher)} is not registered, make sure to call AddCustomLogFlusher() when configuring services.");
        }
    }

    /// <summary>
    /// Gets the global instance of the <see cref="ILogFlusher"/> service.
    /// </summary>
    /// <value>The current log flusher instance.</value>
    /// <exception cref="InvalidOperationException">Thrown if accessed before <see cref="Initialize"/> is called.</exception>
    public static LogFlusher LogFlushService
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
