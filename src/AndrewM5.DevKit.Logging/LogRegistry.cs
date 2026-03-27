using AndrewM5.DevKit.Logging.Contracts.Interfaces;
using System.Collections.Concurrent;

namespace AndrewM5.DevKit.Logging;

internal class LogRegistry : ILogRegistry
{
    public int Count => _logFileQueue.Count;

    private ConcurrentQueue<string> _logFileQueue = new ConcurrentQueue<string>();
    private readonly int _maxEntries;

    public LogRegistry(int maxEntries = 2000)
    {
        if (maxEntries <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxEntries));
        }

        _maxEntries = maxEntries;
    }

    public void EnqueueToLogFileBuffer(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            _logFileQueue.Enqueue(message);
        }
    }

    public string[] DequeueFromLogFileBuffer()
    {
        List<string> list = new List<string>();

        while (_logFileQueue.TryDequeue(out var msg))
        {
            list.Add(msg);
        }

        return list.ToArray();
    }

    public int GetLogFileQueueCount()
    {
        return _logFileQueue.Count;
    }
}
