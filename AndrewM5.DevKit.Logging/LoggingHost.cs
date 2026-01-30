using AndrewM5.DevKit.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System;
namespace AndrewM5.DevKit.Logging;

public static class LoggingHost
{
    private const string NotInitializedMsg = "LoggingHost has not been initialized.";

    private static IServiceProvider? _serviceProvider;
    private static ICustomLoggerManager? _loggerManager;
    private static ILogFlushService? _logFlushService;

    public static void Initialize(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null)
        {
            throw new ArgumentNullException(nameof(serviceProvider));
        }

        _serviceProvider = serviceProvider;

        _loggerManager = _serviceProvider.GetService<ICustomLoggerManager>();
        if (_loggerManager == null)
        {
            throw new InvalidOperationException($"{nameof(ICustomLoggerManager)} is not registered. Make sure you call AddCustomLogging() when configuring services before initializing {nameof(LoggingHost)}.");
        }

        _logFlushService = _serviceProvider.GetService<ILogFlushService>();
        if (_logFlushService == null)
        {
            throw new InvalidOperationException($"{nameof(ILogFlushService)} is not registered. Make sure you call AddCustomLoggingFlushService() when configuring services before initializing {nameof(LoggingHost)}.");
        }
    }

    public static IServiceProvider ServiceProvider
    {
        get
        {
            if (_serviceProvider == null)
            {
                throw new InvalidOperationException(NotInitializedMsg);
            }

            return _serviceProvider;
        }
    }

    public static ICustomLoggerManager LoggerManager
    {
        get
        {
            if (_loggerManager == null)
            {
                throw new InvalidOperationException(NotInitializedMsg);
            }

            return _loggerManager;
        }
    }

    public static ILogFlushService LogFlushService
    {
        get
        {
            if (_logFlushService == null)
            {
                throw new InvalidOperationException(NotInitializedMsg);
            }

            return _logFlushService;
        }
    }

    internal static void Reset()
    {
        _serviceProvider = null;
        _loggerManager = null;
        _logFlushService = null;
    }
}
