using Microsoft.Extensions.Logging;

namespace AndrewM5.DevKit.Logging.Utilities;

internal static class LogFormatter
{
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
