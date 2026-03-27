using AndrewM5.DevKit.Logging.Abstractions;

namespace AndrewM5.DevKit.TaskManagement.Abstractions.Interfaces;

public interface IManagedTaskSnapshot
{
    public string TaskKey { get; }

    public ManagedTaskState State { get; }
    public int IterationCount { get; }
    public DateTime StartTime { get; }
    public DateTime EndTime { get; }
    public TimeSpan Runtime { get; }
    public Exception Exception { get; }

    public void DisplaySnapshot(ICustomLogger? logger = null);
}
