using System.Collections.Concurrent;
using Integration.DevKit.CustomLogger.Contracts;

namespace Integration.DevKit.CustomLogger;


/// <summary>
/// Concrete Implementation of <see cref="ILogRegistry"/>
/// </summary>
internal class LogRegistry : ILogRegistry
{
    /// <inheritdoc />
    public int Count => _logFileQueue.Count;

    private ConcurrentQueue<string> _logFileQueue = new ConcurrentQueue<string>();

    /// <summary>
    /// Initializes a new instance of the <see cref="LogRegistry"/> class with a specified capacity.
    /// </summary>
    public LogRegistry() {}

    /// <inheritdoc />
    /// <remarks>
    /// This implementation ignores null or whitespace strings to ensure log integrity.
    /// </remarks>
    public void EnqueueToLogFileBuffer(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            _logFileQueue.Enqueue(message);
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

        while (_logFileQueue.TryDequeue(out var msg))
        {
            list.Add(msg);
        }

        return list.ToArray();
    }
}
