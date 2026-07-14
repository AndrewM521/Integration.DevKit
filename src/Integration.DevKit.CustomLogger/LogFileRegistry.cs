using System.Collections.Concurrent;
using Integration.DevKit.CustomLogger.Contracts;
using Microsoft.Extensions.Logging;

namespace Integration.DevKit.CustomLogger;


/// <summary>
/// Concrete Implementation of <see cref="ILogFileRegistry"/>
/// </summary>
internal class LogFileRegistry : ILogFileRegistry
{
    /// <inheritdoc />
    public int Count => _logFileRegistry.Count;

    private ConcurrentQueue<string> _logFileRegistry = new ConcurrentQueue<string>();

    /// <summary>
    /// Initializes a new instance of the <see cref="LogFileRegistry"/> class with a specified capacity.
    /// </summary>
    public LogFileRegistry() {}

    /// <inheritdoc />
    /// <remarks>
    /// This implementation ignores null or whitespace strings to ensure log integrity.
    /// </remarks>
    public void EnqueueToLogFileBuffer(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            _logFileRegistry.Enqueue(message);
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
