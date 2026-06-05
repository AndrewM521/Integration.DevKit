
namespace Integration.DevKit.TaskMgmt.Contracts;

/// <summary>
/// Provides access to the context and telemetry of a specific task iteration during its execution.
/// </summary>
/// <remarks>
/// Unlike a snapshot, a handle provides a "live" link to the iteration, allowing for 
/// real-time monitoring of runtime and the ability to trigger cancellation.
/// </remarks>
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
    /// Gets the underlying <see cref="Task"/> object representing the execution of the Managed Task.
    /// </summary>
    /// <remarks>
    /// This represents the total lifetime of the <see cref="ManagedTask"/>. 
    /// To monitor individual execution cycles, refer to the iteration handles or snapshots instead.
    /// </remarks>
    /// <value>The running <see cref="Task"/> if the task has started; otherwise, <see langword="null"/>.</value>
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
    /// </summary>
    public TimeSpan Runtime { get; }

    /// <summary>
    /// Gets the current number of iterations completed or currently in progress.
    /// </summary>
    /// <remarks>
    /// This value is incremented at the start of each new execution cycle within the managed task.
    /// </remarks>
    public int CurrentIterationCount { get; }

    /// <summary>
    /// Initiates a request to cancel the task and all associated workloads.
    /// </summary>
    /// <remarks>
    /// Calling this method signals the root cancellation token for the managed task. 
    /// This signal propagates down to any currently running iteration(s), triggering 
    /// their respective <see cref="IManagedTaskIterationHandle.CancelationToken"/> to stop the 
    /// underlying execution cycles and prevent subsequent iterations 
    /// from starting.
    /// </remarks>
    public void Cancel();
}
