using AndrewM5.DevKit.Logging.Abstractions;
using System.Collections.Concurrent;

namespace AndrewM5.DevKit.TaskManagement.Abstractions;

public interface ITaskScheduleSnapshot {
    public string ScheduleKey { get; }
    public ConcurrentQueue<IManagedTaskSnapshot> Snapshots { get; }

    public void DisplaySnapshot(ICustomLogger? logger = null);
}
