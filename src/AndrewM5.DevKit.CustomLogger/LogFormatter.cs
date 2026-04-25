using Microsoft.Extensions.Logging;

namespace AndrewM5.DevKit.CustomLogger;

/// <summary>
/// Utility method for normalizing and formatting log messages into a standardized string representation.
/// </summary>
public static class LogFormatter
{
    /// <summary>
    /// Formats log data into a structured string, optionally including system metadata like process ID and timestamps.
    /// </summary>
    /// <param name="includePrefix">If set to <see langword="true"/>, prepends process ID, log level, and Unix timestamp to the message.</param>
    /// <param name="category">The source or category of the log (usually the class name).</param>
    /// <param name="message">The primary log message text.</param>
    /// <param name="logLevel">The severity level of the log. Defaults to <see cref="LogLevel.Information"/>.</param>
    /// <param name="ex">An optional <see cref="Exception"/> to append to the end of the formatted string.</param>
    /// <returns>
    /// A formatted string. If <paramref name="includePrefix"/> is <see langword="true"/>, the format is: 
    /// <c>[PID:LEVEL]&lt;UNIX_MS&gt; Category - Message</c>.
    /// </returns>
    /// <remarks>
    /// This method uses <see cref="DateTimeOffset.Now"/> to generate Unix timestamps in milliseconds and 
    /// <see cref="Environment.ProcessId"/> to identify the originating process.
    /// </remarks>
    public static string Format(bool includePrefix, string category, string message, LogLevel logLevel = LogLevel.Information, Exception? ex = null)
    {
        string logLevelStr = "NONE";

        switch (logLevel)
        {
            case LogLevel.Information:
                logLevelStr = "INFO";
                break;
            case LogLevel.Warning:
                logLevelStr = "WARN";
                break;
            case LogLevel.Error:
                logLevelStr = "ERROR";
                break;
            case LogLevel.Critical:
                logLevelStr = "CRIT";
                break;
            case LogLevel.Debug:
                logLevelStr = "DEBUG";
                break;
            case LogLevel.Trace:
                logLevelStr = "TRCE";
                break;
        }

        long timestamp = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        int processId = Environment.ProcessId; 

        string formatted = "";

        if (includePrefix)
        {
            formatted = $"[{processId}:{logLevelStr}]<{timestamp}> {category} - {message}";
        }
        else
        {
            formatted = $"{category} - {message}";
        }

        if (ex != null)
        {
            formatted += Environment.NewLine + ex;
        }

        return formatted;
    }
}
