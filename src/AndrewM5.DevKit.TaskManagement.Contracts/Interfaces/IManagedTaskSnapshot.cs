using AndrewM5.DevKit.Logging.Contracts.Interfaces;

namespace AndrewM5.DevKit.TaskManagement.Abstractions.Interfaces;

/// <summary>
/// Defines a read-only snapshot of a managed task's state and execution metrics 
/// at a specific point in time.
/// </summary>
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
    public DateTime StartTime { get; }

    /// <summary>
    /// Gets the date and time when the task execution concluded. 
    /// If the task is still running, this may represent <see cref="DateTime.MinValue"/> or a default value.
    /// </summary>
    public DateTime EndTime { get; }

    /// <summary>
    /// Gets the total duration the task has been (or was) active.
    /// </summary>
    public TimeSpan Runtime { get; }

    /// <summary>
    /// Gets the exception that caused the task to fail, if the state is <see cref="ManagedTaskState.Faulted"/>.
    /// </summary>
    public Exception Exception { get; }

    /// <summary>
    /// Outputs the snapshot details to the provided logger or the standard system output.
    /// </summary>
    /// <param name="logger">An optional <see cref="ICustomLogger"/> instance to handle the output.</param>
    public void DisplaySnapshot(ICustomLogger? logger = null);
}
