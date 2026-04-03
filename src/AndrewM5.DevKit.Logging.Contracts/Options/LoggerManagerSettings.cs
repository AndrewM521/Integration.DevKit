using Microsoft.Extensions.Logging;

namespace AndrewM5.DevKit.Logging.Abstractions.Options;

/// <summary>
/// Provides configuration settings for managing logging behavior, 
/// specifically defining filtering levels for different output targets.
/// </summary>
public class LoggerManagerSettings
{
    /// <summary>
    /// Gets or sets the minimum <see cref="LogLevel"/> required for log entries 
    /// to be processed in a debugging context.
    /// </summary>
    public LogLevel DebugLogLevel { get; set; } = LogLevel.Debug;

    /// <summary>
    /// Gets or sets the minimum <see cref="LogLevel"/> required for log entries 
    /// to be written to the primary output (e.g., Console or File).
    /// </summary>
    public LogLevel OutputLogLevel { get; set; } = LogLevel.Information;
    
    /// <summary>
    /// Creates a deep copy of the current <see cref="LoggerManagerSettings"/> instance.
    /// </summary>
    /// <returns>A new instance of <see cref="LoggerManagerSettings"/> with the same property values.</returns>
    public LoggerManagerSettings Clone()
    {
        return new LoggerManagerSettings
        {
            DebugLogLevel = DebugLogLevel,
            OutputLogLevel = OutputLogLevel
        };
    }
}
