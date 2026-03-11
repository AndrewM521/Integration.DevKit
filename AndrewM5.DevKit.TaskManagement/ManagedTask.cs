using AndrewM5.DevKit.TaskManagement.Abstractions.Interfaces;

namespace AndrewM5.DevKit.TaskManagement;

public abstract class ManagedTask : IManagedTask
{
    public string TaskName { get; }
    public Guid TaskId { get; }
    public string TaskKey { get; }
    public TimeSpan? Timeout { get; protected set; }

    protected ManagedTask(string taskName, Guid id)
    {
        if (string.IsNullOrWhiteSpace(taskName))
        {
            throw new ArgumentException("Task name cannot be null or whitespace.");
        }

        if (id == Guid.Empty)
        {
            throw new ArgumentException("Task id cannot be empty.");
        }

        TaskName = taskName;
        TaskId = id;
        TaskKey = $"{taskName}_{id}";
    }

    public abstract Task DoTaskWork(CancellationToken cancellationToken);

    public virtual void Dispose() {}
}