namespace AndrewM5.DevKit.TaskManagement.Contracts.Interfaces;

/// <summary>
/// Defines a read-only snapshot of a managed task's state and execution metrics 
/// at a specific point in time.
/// </summary>
/// <remarks>
/// Snapshots are immutable representations of a task's progress. They are ideal for 
/// UI binding, logging, or passing state between service boundaries without 
/// exposing the underlying running task logic.
/// </remarks>
public interface IManagedTaskSnapshot
{
    /// <summary>
    /// Gets the unique identifier for the task associated with this snapshot.
    /// </summary>
    public string TaskKey { get; }

    /// <summary>
    /// Gets the current lifecycle state of the task at the time the snapshot was taken.
    /// </summary>
    public ManagedTaskState State { get; }

    /// <summary>
    /// Gets the number of iterations or cycles the task has completed, 
    /// typically used for recurring or long-running tasks.
    /// </summary>
    public int IterationCount { get; }

    /// <summary>
    /// Gets the date and time when the task execution began.
    /// </summary>
    public DateTime StartDTM { get; }

    /// <summary>
    /// Gets the date and time when the task execution concluded. 
    /// </summary>
    public DateTime EndDTM { get; }

    /// <summary>
    /// Gets the total duration the task has been (or was) active.
    /// </summary>
    public TimeSpan Runtime { get; }

    /// <summary>
    /// Gets the exception that caused the task to fail, if the state is <see cref="ManagedTaskState.Faulted"/>.
    /// </summary>
    /// <value>An <see cref="Exception"/> instance if the task faulted; otherwise, <see langword="null"/></value>
    public Exception? Exception { get; }

    /// <summary>
    /// Gets a historical record of individual iterations completed by this task.
    /// </summary>
    /// <value>
    /// A <see cref="SortedDictionary{Int32, IManagedTaskIterationSnapshot}"/> where the key 
    /// is the iteration index and the value is the performance data for that specific cycle.
    /// </value>
    public SortedDictionary<int, IManagedTaskIterationSnapshot> IterationHistory { get; }

    /// <summary>
    /// Generates a formatted string summarizing the current state and metrics of the task.
    /// </summary>
    /// <param name="showIterations">If <see langword="true"/>, includes detailed history from <see cref="IterationHistory"/> in the output.</param>
    /// <returns>A human-readable string containing the snapshot details.</returns>
    public string GetSnapshotInfo(bool showIterations = false);
}
