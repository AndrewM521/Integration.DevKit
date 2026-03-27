using Microsoft.Extensions.Logging;

namespace AndrewM5.DevKit.Logging.Abstractions.Options;

public class LoggerManagerSettings
{
    public LogLevel DebugLogLevel { get; set; } = LogLevel.Debug;
    public LogLevel OutputLogLevel { get; set; } = LogLevel.Information;

    public LoggerManagerSettings Clone()
    {
        return new LoggerManagerSettings
        {
            DebugLogLevel = DebugLogLevel,
            OutputLogLevel = OutputLogLevel
        };
    }
}
