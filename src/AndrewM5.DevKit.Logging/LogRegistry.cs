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
    private readonly int _maxEntries;

    /// <summary>
    /// Initializes a new instance of the <see cref="LogRegistry"/> class with a specified capacity.
    /// </summary>
    /// <param name="maxEntries">The maximum number of entries the registry is intended to hold. Defaults to 2000.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxEntries"/> is 0 or less.</exception>
    public LogRegistry(int maxEntries = 2000)
    {
        if (maxEntries <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxEntries));
        }

        _maxEntries = maxEntries;
    }

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

    /// <inheritdoc />
    public int GetLogFileQueueCount()
    {
        return _logFileQueue.Count;
    }
}
