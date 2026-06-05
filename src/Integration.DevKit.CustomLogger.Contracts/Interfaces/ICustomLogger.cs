/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.Logging;

namespace Integration.DevKit.CustomLogger.Contracts;

/// <summary>
/// Defines a custom logging interface that extends the standard <see cref="ILogger"/> 
/// with additional controls for managing logger state and output visibility.
/// </summary>
/// <remarks>
/// This interface allows for runtime toggling of logging behavior without needing to 
/// reconfigure the underlying <see cref="ILoggerProvider"/>.
/// </remarks>
public interface ICustomLogger : ILogger
{
    /// <summary>
    /// Gets the category name associated with this logger instance.
    /// </summary>
    public string CategoryName { get; }

    /// <summary>
    /// Gets a value indicating whether the logger is currently enabled.
    /// </summary>
    /// <value><see langword="true"/> if the logger is enabled; otherwise, <see langword="false"/>.</value>
    public bool IsLoggerEnabled { get; }

    /// <summary>
    /// Activates the logger, allowing log messages to be processed.
    /// </summary>
    public void EnableLogger();

    /// <summary>
    /// Deactivates the logger, preventing further log messages from being processed.
    /// </summary>
    /// <remarks>
    /// When disabled, calls to <see cref="ILogger.Log"/> should return immediately without processing.
    /// </remarks>
    public void DisableLogger();

    /// <summary>
    /// Enables the routing of log output to the console.
    /// </summary>
    public void EnableConsoleOutput();

    /// <summary>
    /// Disables the routing of log output to the console.
    /// </summary>
    public void DisableConsoleOutput();
}
