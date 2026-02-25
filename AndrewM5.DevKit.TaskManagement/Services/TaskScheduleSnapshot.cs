using AndrewM5.DevKit.Logging.Abstractions;
using AndrewM5.DevKit.TaskManagement.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace AndrewM5.DevKit.TaskManagement.Services;

public sealed class TaskScheduleSnapshot : ITaskScheduleSnapshot
{
    public string ScheduleKey { get; }

    public ConcurrentQueue<IManagedTaskSnapshot> Snapshots { get; } = new();

    public TaskScheduleSnapshot(string scheduleKey)
    {
        ScheduleKey = scheduleKey;
    }

    public void DisplaySnapshot(ICustomLogger? logger = null)
    {
        foreach (var taskSnapshot in Snapshots)
        {
            logger?.LogDebug($"Schedule Key: {ScheduleKey}");
            taskSnapshot.DisplaySnapshot(logger);
        }
    }
}
