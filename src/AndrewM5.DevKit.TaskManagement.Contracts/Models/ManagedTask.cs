
using AndrewM5.DevKit.TaskManagement.Contracts.Interfaces;

namespace AndrewM5.DevKit.TaskManagement.Contracts.Models;

/// <summary>
/// Provides a base implementation for a task that can be managed by the task management system.
/// Implements basic identification and validation logic.
/// </summary>
public abstract class ManagedTask : IDisposable
{
    /// <summary>
    /// Gets the friendly display name of the task.
    /// </summary>
    public string TaskName { get; }

    /// <summary>
    /// Gets the unique global identifier for this specific task instance.
    /// </summary>
    public Guid TaskId { get; }

    /// <summary>
    /// Gets a unique string key used for lookups within the task registry. 
    /// This is often a combination of the Name and ID.
    /// </summary>
    public string TaskKey { get; }

    /// <summary>
    /// Gets the maximum amount of time the task is allowed to run before being automatically canceled.
    /// If null, the task will run indefinitely until completion or manual cancellation.
    /// </summary>
    public TimeSpan? Timeout { get; protected set; }

    /// <summary>
    /// Gets the monitoring handle associated with this task once it has been queued or started.
    /// This may be null if the task has not yet been registered with the task manager.
    /// </summary>
    public ITaskHandle? Handle { get; protected set; }

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

    /// <summary>
    /// Contains the core logic to be executed by the task manager.
    /// </summary>
    /// <param name="cancellationToken">A token that will be signaled if the task needs to stop (e.g., due to timeout or manual cancellation).</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    /// <remarks>
    /// Both <see cref="ITaskHandle.IterationEndTime"/> and <see cref="ITaskHandle.EndTime"/> are set after this method completes. 
    /// Calling <see cref="ITaskHandle.GetTaskIterationRuntime"/> or <see cref="ITaskHandle.GetTaskRuntime"/> inside this method
    /// will return the runtime between the Start Time and the Current Time
    /// </remarks>
    public abstract Task DoTaskWork(CancellationToken cancellationToken);

    /// <summary>
    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
    /// </summary>
    /// <remarks>
    /// Overriding this method is optional for derived tasks that do not hold onto disposable resources.
    /// </remarks>
    public virtual void Dispose() {}

    public void SetHandle(ITaskHandle handle)
    {
        Handle = handle;
    }
}