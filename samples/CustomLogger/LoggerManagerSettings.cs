using Microsoft.Extensions.Logging;

namespace CustomLogger;

/// <summary>
/// Provides configuration settings for managing logging behavior, 
/// specifically defining filtering levels for different output targets.
/// </summary>
/// <remarks>
/// These settings allow for independent control over what is captured for 
/// immediate debugging versus what is persisted to primary output.
/// </remarks>
public class LoggerManagerSettings
{
    /// <summary>
    /// Gets or sets the minimum <see cref="LogLevel"/> required for log entries 
    /// to be processed in a debugging context.
    /// </summary>
    /// <value>The default is <see cref="LogLevel.Debug"/>.</value>
    /// <remarks>
    /// Messages below this level will be ignored.
    /// </remarks>
    public LogLevel OutputLogLevel { get; set; } = LogLevel.Debug;

    /// <summary>
    /// Gets or sets the minimum <see cref="LogLevel"/> required for log entries 
    /// to be written to the file output
    /// </summary>
    /// <value>The default is <see cref="LogLevel.Information"/>.</value>
    /// <remarks>
    /// Messages below this level will be ignored.
    /// </remarks>
    public LogLevel FileOutputLogLevel { get; set; } = LogLevel.Information;
    
    /// <summary>
    /// Creates a deep copy of the current <see cref="LoggerManagerSettings"/> instance.
    /// </summary>
    /// <returns>A new instance of <see cref="LoggerManagerSettings"/> with the same property values.</returns>
    public LoggerManagerSettings Clone()
    {
        return new LoggerManagerSettings
        {
            OutputLogLevel = OutputLogLevel,
            FileOutputLogLevel = FileOutputLogLevel
        };
    }
}
