namespace AndrewM5.DevKit.TaskManagement.Abstractions.Interfaces;

public interface IManagedTask : IDisposable
{
    public string TaskName { get; }
    public Guid TaskId { get; }
    public string TaskKey { get; }
    public TimeSpan? Timeout { get; }

    public abstract Task DoTaskWork(CancellationToken cancellationToken);
}