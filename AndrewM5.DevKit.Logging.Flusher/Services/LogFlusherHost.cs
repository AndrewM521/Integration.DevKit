using AndrewM5.DevKit.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.Logging.Flusher.Services;

public static class LogFlusherHost
{
    private static ILogFlusher? _logFlushService;

    private const string NoInit = "LogFlusherHost has not been initialized.";

    public static void Initalize(IServiceProvider sp)
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
            throw new InvalidOperationException($"{nameof(LogFlusherServiceCollection)} requires the Logging module. Call AddCustomLogging() before AddCustomLogFlusher()");
        }

        _logFlushService = sp.GetService<ILogFlusher>();
        if (_logFlushService == null)
        {
            throw new InvalidOperationException($"{nameof(ILogFlusher)} is not registered, make sure to call AddCustomLogFlusher() when configuring services.");
        }
    }

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
