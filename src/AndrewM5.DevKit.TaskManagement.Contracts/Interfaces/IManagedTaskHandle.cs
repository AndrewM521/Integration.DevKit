using AndrewM5.DevKit.Core.Results;

namespace AndrewM5.DevKit.TaskManagement.Contracts.Interfaces;

/// <summary>
/// Provides a handle for monitoring and interacting with a task 
/// that has been queued or started by the task manager.
/// </summary>
public interface IManagedTaskHandle
{
    /// <summary>
    /// Gets the unique string identifier associated with the task.
    /// </summary>
    public string TaskKey { get; }

    /// <summary>
    /// Gets the current lifecycle state of the task.
    /// </summary>
    public ManagedTaskState State { get; }

    /// <summary>
    /// The <see cref="Task"/> object of the Managed Task
    /// </summary>
    /// <remarks>
    /// Note: This is NOT the <see cref="Task"/> object for each iteration, this is 
    /// the lifetime <see cref="Task"/> for the custom <see cref="Models.ManagedTask"/>
    /// </remarks>
    public Task? RunningTask { get; }

    /// <summary>
    /// Gets the UTC timestamp of when the task started.
    /// </summary>
    public DateTime StartDTM { get; }

    /// <summary>
    /// Gets the UTC timestamp of when the task ended.
    /// </summary>
    public DateTime EndDTM { get; }

    /// <summary>
    /// Gets the calculated duration of the task. 
    /// If the task is still active, it returns the duration from <see cref="StartDTM"/> to current UTC time.
    /// </summary>
    public TimeSpan Runtime { get; }

    /// <summary>
    /// Gets the current iteration count
    /// </summary>
    public int CurrentIterationCount { get; }
}
