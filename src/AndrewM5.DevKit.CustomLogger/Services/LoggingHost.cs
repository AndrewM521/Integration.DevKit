using AndrewM5.DevKit.CustomLogger.Contracts.Interfaces;
using Microsoft.Extensions.DependencyInjection;
namespace AndrewM5.DevKit.Logging.Services;

/// <summary>
/// Provides a static entry point to access the Logging module. 
/// </summary>
/// <remarks>
/// This host acts as a static wrapper for services resolved from the Dependency Injection container. 
/// It must be initialized during application startup (e.g., in Program.cs or Startup.cs) 
/// after the service provider has been built.
/// </remarks>
public static class LoggingHost
{
    private const string NoInit = "LoggingHost has not been initialized.";

    private static ILogRegistry? _logRegistry;
    private static ICustomLoggerManager? _loggerManager;

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
