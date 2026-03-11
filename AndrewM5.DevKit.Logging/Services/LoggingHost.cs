using AndrewM5.DevKit.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
namespace AndrewM5.DevKit.Logging.Services;

public static class LoggingHost
{
    private const string NoInit = "LoggingHost has not been initialized.";

    private static ILogRegistry? _logRegistry;
    private static ICustomLoggerManager? _loggerManager;
    
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
