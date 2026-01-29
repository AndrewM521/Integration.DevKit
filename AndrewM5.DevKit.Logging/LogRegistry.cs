using System.Collections.Concurrent;

namespace AndrewM5.DevKit.Logging;

public static class LogRegistry
{
    private static readonly ConcurrentQueue<string> _logFileQueue = new ConcurrentQueue<string>();

    public static void EnqueueToLogFileBuffer(string message)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            _logFileQueue.Enqueue(message);
        }
    }
    public static string[] DequeueFromLogFileBuffer()
    {
        List<string> list = new List<string>();

        while (_logFileQueue.TryDequeue(out var msg))
        {
            list.Add(msg);
        }

        return list.ToArray();
    }

    public static int GetLogFileQueueCount()
    {
        return _logFileQueue.Count;
    }
}
