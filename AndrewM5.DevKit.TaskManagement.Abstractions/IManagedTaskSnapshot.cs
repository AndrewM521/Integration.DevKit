using AndrewM5.DevKit.Logging.Abstractions;

namespace AndrewM5.DevKit.TaskManagement.Abstractions;

public interface IManagedTaskSnapshot
{
    public string TaskKey { get; }

    public ManagedTaskState State { get; }
    public DateTime StartUtc { get; }
    public DateTime EndUtc { get; }
    
    public TimeSpan Runtime { get; }
    public string? ErrorMessage { get; }
    public string? ErrorType { get; }

    public void DisplaySnapshot(ICustomLogger? logger = null);
}
