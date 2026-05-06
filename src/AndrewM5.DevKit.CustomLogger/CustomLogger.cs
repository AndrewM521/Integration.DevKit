/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using AndrewM5.DevKit.CustomLogger.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace AndrewM5.DevKit.CustomLogger;

/// <summary>
/// A custom implementation of <see cref="ILogger"/> and <see cref="ICustomLogger"/> that 
/// routes log messages to a central registry, the debug output, and optionally the console.
/// </summary>
public class CustomLogger : ICustomLogger
{
    private readonly ICustomLoggerManager _loggerManager;
    private readonly ILogRegistry _logRegistry;

    private bool _isLoggerEnabled = true;
    private string _categoryName = string.Empty;
    private bool _outputToConsole = false;

    /// <inheritdoc />
    public string CategoryName => _categoryName;

    /// <inheritdoc />
    public bool IsLoggerEnabled => _isLoggerEnabled;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomLogger"/> class.
    /// </summary>
    /// <param name="loggerManager">The manager providing runtime settings and configuration.</param>
    /// <param name="logRegistry">The registry where formatted log strings are buffered for persistence.</param>
    /// <param name="categoryName">The name of the category for this logger (e.g., the class name).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="loggerManager"/> is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="logRegistry"/> is null.</exception>
    internal CustomLogger(ICustomLoggerManager loggerManager, ILogRegistry logRegistry, string categoryName = "Unknown Category")
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
        _logRegistry = logRegistry;
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

        _logRegistry.EnqueueToLogFileBuffer(formattedMsg);

        Debug.WriteLine(formattedMsg);

        if (_outputToConsole)
        {
            Console.WriteLine(LogFormatter.Format(false, CategoryName, message, logLevel, exception));
        }
    }

    /// <inheritdoc />
    public void EnableLogger()
    {
        _isLoggerEnabled = true;
    }

    /// <inheritdoc />
    public void DisableLogger()
    {
        _isLoggerEnabled = false;
    }

    /// <inheritdoc />
    public void EnableConsoleOutput()
    {
        _outputToConsole = true;
    }

    /// <inheritdoc />
    public void DisableConsoleOutput()
    {
        _outputToConsole = false;
    }
}

/// <summary>
/// A no-op implementation of <see cref="IDisposable"/> used to satisfy the <see cref="ILogger.BeginScope"/> contract.
/// </summary>
internal sealed class NullScope : IDisposable
{
    public static readonly NullScope Instance = new NullScope();
    private NullScope() { }
    public void Dispose() { }
}