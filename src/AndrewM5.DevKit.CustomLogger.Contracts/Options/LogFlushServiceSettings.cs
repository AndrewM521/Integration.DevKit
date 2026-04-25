namespace AndrewM5.DevKit.CustomLogger.Contracts.Options;

/// <summary>
/// Defines the configuration settings for the log flushing service, 
/// controlling how and when log data is persisted to disk.
/// </summary>
public class LogFlushServiceSettings
{
    /// <summary>
    /// Gets or sets a value indicating whether a physical log file should be created.
    /// </summary>
    /// <value>Default is <see langword="false"/>.</value>
    public bool CreateLogFile { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the service is permitted 
    /// to create files when running inside a containerized environment (e.g., Docker).
    /// </summary>
    /// <remarks>
    /// Writing to a container's ephemeral storage is generally discouraged in favor 
    /// of volumes or external log collectors.
    /// </remarks>
    public bool AllowCreateFileInContainer { get; set; } = false;

    /// <summary>
    /// Gets or sets the maximum number of log entries to hold in memory 
    /// before triggering a mandatory flush to the destination.
    /// </summary>
    public int MaxBufferCount { get; set; } = 50;

    /// <summary>
    /// Gets or sets the time interval, in seconds, at which the buffer 
    /// is automatically flushed regardless of the current buffer count.
    /// </summary>
    /// <value>The interval in seconds. Default is 30 seconds.</value>
    public int FlushIntervalSeconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the full directory path or filename where logs will be stored.
    /// </summary>
    /// <remarks>
    /// If <see cref="CreateLogFile"/> is <see langword="false"/>, this setting is ignored.
    /// </remarks>
    public string LogFilePath { get; set; } = string.Empty;

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
