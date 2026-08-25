using System.Collections.Concurrent;
using Integration.DevKit.CustomLogger.Contracts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Integration.DevKit.CustomLogger;


/// <summary>
/// Concrete Implementation of <see cref="ILogFileRegistry"/>
/// </summary>
internal class LogFileRegistry : ILogFileRegistry
{
    private const int DefaultMaxBufferCount = 50;

    /// <summary>
    /// Gets the number of pending log messages currently buffered in the registry.
    /// </summary>
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

    /// <inheritdoc />
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

    /// <inheritdoc />
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
