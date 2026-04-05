using AndrewM5.DevKit.TaskManagement.Abstractions.Interfaces;

namespace AndrewM5.DevKit.TaskManagement;

/// <summary>
/// Provides a base implementation for a task that can be managed by the task management system.
/// Implements basic identification and validation logic.
/// </summary>
public abstract class ManagedTask : IManagedTask
{
    /// <inheritdoc />
    public string TaskName { get; }

    /// <inheritdoc />
    public Guid TaskId { get; }

    /// <inheritdoc />
    public string TaskKey { get; }

    /// <inheritdoc />
    public TimeSpan? Timeout { get; protected set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedTask"/> class.
    /// </summary>
    /// <param name="taskName">The friendly display name of the task.</param>
    /// <param name="id">A unique identifier for this specific task instance.</param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="taskName"/> is null/whitespace or <paramref name="id"/> is <see cref="Guid.Empty"/>.</exception>
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

    /// <inheritdoc />
    public abstract Task DoTaskWork(CancellationToken cancellationToken);

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <remarks>
    /// Overriding this method is optional for derived tasks that do not hold onto disposable resources.
    /// </remarks>
    public virtual void Dispose() {}
}