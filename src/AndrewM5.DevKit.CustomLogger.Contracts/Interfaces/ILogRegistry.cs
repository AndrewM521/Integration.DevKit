namespace AndrewM5.DevKit.CustomLogger.Contracts.Interfaces;

/// <summary>
/// Defines a central registry that acts as a buffer for log messages, 
/// facilitating the hand-off between log producers and the log flushing service.
/// </summary>
public interface ILogRegistry
{
    /// <summary>
    /// Gets the total number of log messages currently residing in the registry.
    /// </summary>
    /// <value>An <see cref="int"/> representing the count of pending log entries.</value>
    public int Count { get; }

    /// <summary>
    /// Adds a new log message to the internal buffer destined for file output.
    /// </summary>
    /// <param name="message">The formatted log entry to be stored.</param>
    /// <remarks>
    /// This method is the primary entry point for log producers.
    /// </remarks>
    public void EnqueueToLogFileBuffer(string message);

    /// <summary>
    /// Retrieves and removes all currently buffered log messages for file output.
    /// </summary>
    /// <returns>
    /// An array of <see cref="string"/> containing all log messages that were in the buffer. 
    /// If the buffer is empty, returns an empty array.
    /// </returns>
    /// <remarks>
    /// This is typically called by an <see cref="ILogFlusher"/> to clear the buffer 
    /// and commit entries to persistent storage.
    /// </remarks>
    public string[] DequeueFromLogFileBuffer();
}
