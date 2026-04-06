using AndrewM5.DevKit.Logging.Contracts.Interfaces;
using System.Collections.Concurrent;

namespace AndrewM5.DevKit.Logging;

/// <summary>
/// An internal implementation of <see cref="ILogRegistry"/> that manages an in-memory 
/// thread-safe buffer for log messages using a <see cref="ConcurrentQueue{T}"/>.
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
    /// This method performs a destructive read, clearing the internal queue as it 
    /// populates the return array.
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
