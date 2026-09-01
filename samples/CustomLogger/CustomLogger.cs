using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace CustomLogger;

/// <summary>
/// A custom implementation of <see cref="ILogger"/> that routes log messages to a central registry, the debug output, and optionally the console.
/// </summary>
public class CustomLogger : ILogger
{
    private readonly CustomLoggerManager _loggerManager;
    private readonly LogFileRegistry _logFileRegistry;

    private bool _isLoggerEnabled = true;
    private string _categoryName = string.Empty;
    private bool _outputToConsole = false;

    /// <summary>
    /// Gets the category name associated with this logger instance.
    /// </summary>
    public string CategoryName => _categoryName;

    /// <summary>
    /// Gets a value indicating whether the logger is currently enabled.
    /// </summary>
    /// <value><see langword="true"/> if the logger is enabled; otherwise, <see langword="false"/>.</value>
    public bool IsLoggerEnabled => _isLoggerEnabled;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomLogger"/> class.
    /// </summary>
    /// <param name="loggerManager">The manager providing runtime settings and configuration.</param>
    /// <param name="logRegistry">The registry where formatted log strings are buffered for persistence.</param>
    /// <param name="categoryName">The name of the category for this logger (e.g., the class name).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="loggerManager"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logRegistry"/> is null.</exception>
    internal CustomLogger(CustomLoggerManager loggerManager, LogFileRegistry logRegistry, string categoryName = "Unknown Category")
    {
        if (loggerManager == null)
        {
            throw new ArgumentNullException(nameof(loggerManager));
        }

        if (logRegistry == null)
        {
            throw new ArgumentNullException(nameof(logRegistry));
        }

        _loggerManager = loggerManager;
        _logFileRegistry = logRegistry;
        _categoryName = categoryName;
    }

    /// <summary>
    /// Begins a logical operation scope.
    /// </summary>
    /// <remarks>
    /// This implementation returns a <see cref="NullScope"/> as scoped logging is not currently supported.
    /// </remarks>
    /// <typeparam name="TState">The type of the state to begin scope for.</typeparam>
    /// <param name="state">The identifier for the scope.</param>
    /// <returns>An <see cref="IDisposable"/> that ends the logical operation scope on dispose.</returns>
    public IDisposable BeginScope<TState>(TState state) where TState : notnull
    {
        return NullScope.Instance;
    }

    /// <summary>
    /// Determines if the logger is enabled based on the current <see cref="IsLoggerEnabled"/> 
    /// state and the minimum <see cref="LogLevel"/> defined in <see cref="ICustomLoggerManager.RuntimeSettings"/>.
    /// </summary>
    /// <param name="logLevel">The level to check.</param>
    /// <returns><see langword="true"/> if the logger is enabled for the specified level; otherwise, <see langword="false"/>.</returns>
    public bool IsEnabled(LogLevel logLevel)
    {
        if (_isLoggerEnabled && logLevel >= _loggerManager.RuntimeSettings.OutputLogLevel)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// Writes a log entry to the configured outputs
    /// </summary>
    /// <inheritdoc cref="ILogger.Log{TState}(LogLevel, EventId, TState, Exception?, Func{TState, Exception?, string})"/>
    /// <remarks>
    /// Messages are automatically formatted using an internal <c>LogFormatter</c> before being enqueued.
    /// </remarks>
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        if (!IsEnabled(logLevel))
        {
            return;
        }

        string message = formatter(state, exception);

        string formattedMsg = LogFormatter.Format(true, CategoryName, message, logLevel, exception);

        if (logLevel >= _loggerManager.RuntimeSettings.FileOutputLogLevel)
        {
            _logFileRegistry.EnqueueToLogFileBuffer(formattedMsg);
        }

        Debug.WriteLine(formattedMsg);

        if (_outputToConsole)
        {
            Console.WriteLine(LogFormatter.Format(false, CategoryName, message, logLevel, exception));
        }
    }

    /// <summary>
    /// Activates the logger, allowing log messages to be processed.
    /// </summary>
    public void EnableLogger()
    {
        _isLoggerEnabled = true;
    }

    /// <summary>
    /// Deactivates the logger, preventing further log messages from being processed.
    /// </summary>
    /// <remarks>
    /// When disabled, calls to <see cref="ILogger.Log"/> should return immediately without processing.
    /// </remarks>
    public void DisableLogger()
    {
        _isLoggerEnabled = false;
    }

    /// <summary>
    /// Enables the routing of log output to the console.
    /// </summary>
    public void EnableConsoleOutput()
    {
        _outputToConsole = true;
    }

    /// <summary>
    /// Disables the routing of log output to the console.
    /// </summary>
    public void DisableConsoleOutput()
    {
        _outputToConsole = false;
    }
}

/// <summary>
/// Represents a no-op scope used when logical operation scoping is not supported.
/// </summary>
internal sealed class NullScope : IDisposable
{
    public static readonly NullScope Instance = new NullScope();

    /// <summary>
    /// Initializes a no-op scope used when logical operation scoping is not supported.
    /// </summary>
    private NullScope() { }
    public void Dispose() { }
}