using AndrewM5.DevKit.Logging.Abstractions;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AndrewM5.DevKit.Logging;

public class CustomLogger : ICustomLogger
{
    private readonly ICustomLoggerManager _loggerManager;
    private readonly ILogRegistry _logRegistry;

    private bool _isLoggerEnabled = true;
    private string _categoryName = string.Empty;
    private bool _outputToConsole = false;

    public string CategoryName => _categoryName;
    public bool IsLoggerEnabled => _isLoggerEnabled;

    public CustomLogger(ICustomLoggerManager loggerManager, ILogRegistry logRegistry, string categoryName = "Unknown Category")
    {
        if (loggerManager == null)
        {
            throw new ArgumentNullException(nameof(loggerManager));
        }

        _loggerManager = loggerManager;
        _logRegistry = logRegistry;
        _categoryName = categoryName;
    }

    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        return NullScope.Instance;
    }

    public bool IsEnabled(LogLevel logLevel)
    {
        if (_isLoggerEnabled && logLevel >= _loggerManager.RuntimeSettings.DebugLogLevel)
        {
            return true;
        }

        return false;
    }

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        string message = formatter(state, exception);

        string formattedMsg = LogFormatter.Format(true, CategoryName, message, logLevel, exception);

        _logRegistry.EnqueueToLogFileBuffer(formattedMsg);

        Debug.WriteLine(formattedMsg);

        if (_outputToConsole)
        {
            Console.WriteLine(LogFormatter.Format(false, CategoryName, message, logLevel, exception));
        }
    }

    public void EnableLogger()
    {
        _isLoggerEnabled = true;
    }
    public void DisableLogger()
    {
        _isLoggerEnabled = false;
    }

    public void EnableConsoleOutput()
    {
        _outputToConsole = true;
    }
    public void DisableConsoleOutput()
    {
        _outputToConsole = false;
    }
}

internal sealed class NullScope : IDisposable
{
    public static readonly NullScope Instance = new NullScope();
    private NullScope() { }
    public void Dispose() { }
}