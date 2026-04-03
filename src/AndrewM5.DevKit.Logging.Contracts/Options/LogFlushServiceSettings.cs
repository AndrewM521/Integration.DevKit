namespace AndrewM5.DevKit.Logging.Abstractions.Options;

/// <summary>
/// Defines the configuration settings for the log flushing service, 
/// controlling how and when log data is persisted to disk.
/// </summary>
public class LogFlushServiceSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether a physical log file should be created.
    /// </summary>
    public bool CreateLogFile { get; set; } = false;

    /// <summary>
    /// Gets or sets the full directory path or filename where logs will be stored.
    /// </summary>
    public string LogFilePath { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the maximum number of log entries to hold in memory 
    /// before triggering a mandatory flush to the destination.
    /// </summary>
    public int MaxBufferCount { get; set; } = 50;

    /// <summary>
    /// Gets or sets the time interval, in seconds, at which the buffer 
    /// is automatically flushed regardless of the current buffer count.
    /// </summary>
    public int FlushIntervalSeconds { get; set; } = 5;

    /// <summary>
    /// Gets or sets a value indicating whether the service is permitted 
    /// to create files when running inside a containerized environment (e.g., Docker).
    /// </summary>
    public bool AllowCreateFileInContainer { get; set; } = false;

    /// <summary>
    /// Creates a deep copy of the current <see cref="LogFlushServiceSettings"/> instance.
    /// </summary>
    /// <returns>A new instance of <see cref="LogFlushServiceSettings"/> with the same property values.</returns>
    public LogFlushServiceSettings Clone()
    {
        return new LogFlushServiceSettings
        {
            CreateLogFile = CreateLogFile,
            LogFilePath = LogFilePath,
            MaxBufferCount = MaxBufferCount,
            FlushIntervalSeconds = FlushIntervalSeconds,
            AllowCreateFileInContainer = AllowCreateFileInContainer,
        };
    }
}
