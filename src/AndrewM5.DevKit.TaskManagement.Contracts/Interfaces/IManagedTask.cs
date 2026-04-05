namespace AndrewM5.DevKit.TaskManagement.Abstractions.Interfaces;

/// <summary>
/// Defines the contract for a task that can be executed and monitored by the task management system.
/// Implements <see cref="IDisposable"/> to ensure resource cleanup after execution.
/// </summary>
public interface IManagedTask : IDisposable
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
    public TimeSpan? Timeout { get; }

    /// <summary>
    /// Contains the core logic to be executed by the task manager.
    /// </summary>
    /// <param name="cancellationToken">A token that will be signaled if the task needs to stop (e.g., due to timeout or manual cancellation).</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public abstract Task DoTaskWork(CancellationToken cancellationToken);
}