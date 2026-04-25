using AndrewM5.DevKit.TaskManagement.Contracts.Interfaces;
using AndrewM5.DevKit.TaskManagement.Contracts.Models;

namespace AndrewM5.DevKit.TaskManagement;

/// <summary>
/// Concrete implementation of <see cref="IManagedTaskSnapshot"/> providing a 
/// thread-safe, read-only view of a task's metrics and state.
/// </summary>
public sealed class ManagedTaskSnapshot : IManagedTaskSnapshot
{
    /// <inheritdoc/>
    public string TaskKey { get; init; } = "";

    /// <summary>
    /// Gets the configuration settings applied to the task at the time of the snapshot.
    /// </summary>
    public ManagedTaskSettings? Settings { get; init; }

    /// <inheritdoc/>
    public ManagedTaskState State { get; internal set; }

    /// <inheritdoc/>
    public int IterationCount { get; internal set; }

    /// <inheritdoc/>
    public DateTime StartDTM { get; internal set; }

    /// <inheritdoc/>
    public DateTime EndDTM { get; internal set; }

    /// <inheritdoc/>
    public TimeSpan Runtime { get; internal set; }

    /// <inheritdoc/>
    public Exception? Exception { get; internal set; }

    /// <inheritdoc/>
    public SortedDictionary<int, IManagedTaskIterationSnapshot> IterationHistory { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedTaskSnapshot"/> class.
    /// </summary>
    /// <param name="taskKey">The unique identifier for the task.</param>
    /// <param name="settings">The settings the task is running with.</param>
    public ManagedTaskSnapshot(string taskKey, ManagedTaskSettings settings)
    {
        TaskKey = taskKey;
        Settings = settings;
    }

    /// <summary>
    /// Generates a formatted summary string of the task's current metrics and state.
    /// </summary>
    /// <param name="showIterations">If set to <c>true</c>, appends the detailed info of every iteration in the <see cref="IterationHistory"/>.</param>
    /// <returns>A formatted string representing the task's point-in-time status.</returns>
    public string GetSnapshotInfo(bool showIterations = false)
    {
        string msg = @$"
        --- Task Snapshot ---
        TaskKey: {TaskKey}
        State: {State}
        IterationCount: {IterationCount}
        StartUtc: {StartDTM}
        EndUtc: {EndDTM}
        Runtime: {Runtime}
        ExceptionType: {Exception?.GetType().Name ?? "None"}
        ExceptionMessage: {Exception?.Message ?? "None"}
        ";

        if (showIterations)
        {
            foreach (var iteration in IterationHistory.Values)
            {
                msg += iteration.GetIterationInfo(true);
            }
        }

        return msg;
    }
}
