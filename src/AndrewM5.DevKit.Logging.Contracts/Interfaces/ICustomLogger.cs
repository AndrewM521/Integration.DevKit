using Microsoft.Extensions.Logging;

namespace AndrewM5.DevKit.Logging.Contracts.Interfaces;

/// <summary>
/// Defines a custom logging interface that extends the standard <see cref="ILogger"/> 
/// with additional controls for managing logger state and output visibility.
/// </summary>
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
    public void DisableLogger();

    /// <summary>
    /// Enables the routing of log output specifically to the console.
    /// </summary>
    public void EnableConsoleOutput();

    /// <summary>
    /// Disables the routing of log output to the console without affecting other sinks.
    /// </summary>
    public void DisableConsoleOutput();
}
