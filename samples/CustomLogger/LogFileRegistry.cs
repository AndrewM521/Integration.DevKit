using System.Collections.Concurrent;
using Microsoft.Extensions.Options;

namespace CustomLogger;

/// <summary>
/// Defines a central registry that acts as a buffer for log messages, 
/// facilitating the hand-off between log producers and the log flushing service.
/// </summary>
public class LogFileRegistry
{
    private const int DefaultMaxBufferCount = 50;

    /// <summary>
    /// Gets the total number of log messages currently residing in the registry.
    /// </summary>
    /// <value>An <see cref="int"/> representing the count of pending log entries.</value>
    public int Count => _logFileRegistry.Count;

    private readonly ConcurrentQueue<string> _logFileRegistry = new ConcurrentQueue<string>();
    private readonly int _maxBufferCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogFileRegistry"/> class.
    /// </summary>
    /// <param name="flushSettings">
    /// The flush service settings, whose <see cref="LogFlushServiceSettings.MaxBufferCount"/> is enforced here as a
    /// hard cap on the in-memory buffer, independent of whether the flush service is actually draining it. Resolves
    /// to defaults if <c>AddCustomLogFlusher</c> was never called.
    /// </param>
    public LogFileRegistry(IOptions<LogFlushServiceSettings> flushSettings)
    {
        _maxBufferCount = flushSettings.Value.MaxBufferCount > 0 ? flushSettings.Value.MaxBufferCount : DefaultMaxBufferCount;
    }

    /// <summary>
    /// Adds a new log message to the internal buffer destined for file output.
    /// </summary>
    /// <param name="message">The formatted log entry to be stored.</param>
    /// <remarks>
    /// This method is the primary entry point for log producers.
    /// </remarks>
    /// <remarks>
    /// This implementation ignores null or whitespace strings to ensure log integrity. If the buffer is at
    /// capacity (<see cref="LogFlushServiceSettings.MaxBufferCount"/>), the oldest buffered message is dropped
    /// to make room — this keeps the buffer bounded even if nothing is currently draining it (e.g.
    /// <see cref="LogFlushServiceSettings.CreateLogFile"/> is <see langword="false"/>, or the flush service
    /// was never registered at all).
    /// </remarks>
    public void EnqueueToLogFileBuffer(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        _logFileRegistry.Enqueue(message);

        while (_logFileRegistry.Count > _maxBufferCount && _logFileRegistry.TryDequeue(out _))
        {
            // Drop oldest buffered messages to keep the registry bounded.
        }
    }

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
    /// <remarks>
    /// This method performs a destructive read, draining the internal queue entirely. 
    /// Messages are returned in the order they were enqueued (FIFO).
    /// </remarks>
    public string[] DequeueFromLogFileBuffer()
    {
        List<string> list = new List<string>();

        while (_logFileRegistry.TryDequeue(out var msg))
        {
            list.Add(msg);
        }

        return list.ToArray();
    }
}
