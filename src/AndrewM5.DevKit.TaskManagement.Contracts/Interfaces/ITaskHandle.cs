using AndrewM5.DevKit.Core.Results;

namespace AndrewM5.DevKit.TaskManagement.Abstractions.Interfaces;

/// <summary>
/// Provides a handle for monitoring and interacting with a task 
/// that has been queued or started by the task manager.
/// </summary>
public interface ITaskHandle
{
    /// <summary>
    /// Gets the unique string identifier associated with the task.
    /// </summary>
    public string TaskKey { get; }

    /// <summary>
    /// Gets the current lifecycle state of the task (e.g., Running, Completed, Canceled).
    /// </summary>
    public ManagedTaskState State { get; }

    /// <summary>
    /// Gets the underlying <see cref="Task"/> representing the actual execution of the workload.
    /// This may be null if the task has not yet been dispatched or fails to start.
    /// </summary>
    public Task? RunningTask { get; }

    /// <summary>
    /// Calculates the current duration the task has been active.
    /// </summary>
    /// <returns>An <see cref="OperationResult{TimeSpan}"/> containing the elapsed runtime.</returns>
    public OperationResult<TimeSpan> GetTaskRuntime();
}
