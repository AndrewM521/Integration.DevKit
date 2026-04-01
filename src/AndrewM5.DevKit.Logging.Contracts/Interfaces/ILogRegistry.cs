namespace AndrewM5.DevKit.Logging.Contracts.Interfaces;

/// <summary>
/// Defines a central registry that acts as a buffer for log messages, 
/// facilitating the hand-off between log producers and the log flushing service.
/// </summary>
public interface ILogRegistry
{
    /// <summary>
    /// Gets the total number of log messages currently residing in the registry.
    /// </summary>
    public int Count { get; }

    /// <summary>
    /// Adds a new log message to the internal buffer destined for file output.
    /// </summary>
    /// <param name="message">The formatted log entry to be stored.</param>
    public void EnqueueToLogFileBuffer(string message);

    /// <summary>
    /// Retrieves and removes all currently buffered log messages for file output.
    /// </summary>
    /// <returns>An array of log messages to be written to the log file.</returns>
    /// <remarks>
    /// This is typically called by an <see cref="ILogFlusher"/> to clear the buffer 
    /// and commit entries to persistent storage.
    /// </remarks>
    public string[] DequeueFromLogFileBuffer();

    /// <summary>
    /// Gets the current number of items waiting in the log file buffer.
    /// </summary>
    /// <returns>The count of pending log entries.</returns>
    public int GetLogFileQueueCount();
}
