/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.TaskMgmt.Interfaces;
using Integration.DevKit.TaskMgmt.Models;

namespace Integration.DevKit.TaskMgmt;

/// <summary>
/// Read-only snapshot of a managed task's state and execution metrics
/// at a specific point in time, providing a thread-safe view of a task's metrics and state.
/// </summary>
/// <remarks>
/// Snapshots are immutable representations of a task's progress. They are ideal for
/// UI binding, logging, or passing state between service boundaries without
/// exposing the underlying running task logic.
/// </remarks>
public sealed class ManagedTaskSnapshot
{
    /// <summary>
    /// Gets the unique identifier for the task associated with this snapshot.
    /// </summary>
    public string TaskKey { get; init; } = "";

    /// <summary>
    /// Gets the configuration settings applied to the task at the time of the snapshot.
    /// </summary>
    public ManagedTaskSettings? Settings { get; init; }

    /// <summary>
    /// Gets the current lifecycle state of the task at the time the snapshot was taken.
    /// </summary>
    public ManagedTaskState State { get; internal set; }

    /// <summary>
    /// Gets the number of iterations or cycles the task has completed,
    /// typically used for recurring or long-running tasks.
    /// </summary>
    public long IterationCount { get; internal set; }

    /// <summary>
    /// Gets the date and time when the task execution began.
    /// </summary>
    public DateTime StartDTM { get; internal set; }

    /// <summary>
    /// Gets the date and time when the task execution concluded.
    /// </summary>
    public DateTime EndDTM { get; internal set; }

    /// <summary>
    /// Gets the total duration the task has been (or was) active.
    /// </summary>
    public TimeSpan Runtime { get; internal set; }

    /// <summary>
    /// Gets the exception that caused the task to fail, if the state is <see cref="ManagedTaskState.Faulted"/>.
    /// </summary>
    /// <value>An <see cref="Exception"/> instance if the task faulted; otherwise, <see langword="null"/></value>
    public Exception? Exception { get; internal set; }

    /// <summary>
    /// The live iteration history, keyed by iteration number. Parallel iterations update it concurrently,
    /// so every access must <see langword="lock"/> it; callers outside the runtime use <see cref="GetIterationHistory"/>.
    /// </summary>
    internal SortedDictionary<long, IManagedTaskIterationSnapshot> IterationHistory { get; } = new SortedDictionary<long, IManagedTaskIterationSnapshot>();

    /// <summary>
    /// Gets a copy of the iterations recorded for this task, oldest first.
    /// </summary>
    /// <returns>
    /// A point-in-time copy, safe to enumerate while the task is still running. The number kept is limited by
    /// <see cref="Settings.TaskManagerSettings.MaxTaskIterationRegistryCount"/>.
    /// </returns>
    public IReadOnlyList<IManagedTaskIterationSnapshot> GetIterationHistory()
    {
        lock (IterationHistory)
        {
            return new List<IManagedTaskIterationSnapshot>(IterationHistory.Values);
        }
    }

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
    /// Generates a formatted string summarizing the current state and metrics of the task.
    /// </summary>
    /// <param name="showIterations">If <see langword="true"/>, includes detailed history from <see cref="GetIterationHistory"/> in the output.</param>
    /// <returns>A human-readable string containing the snapshot details.</returns>
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
            // Same lock the registry takes while it updates the history.
            lock (IterationHistory)
            {
                foreach (var iteration in IterationHistory.Values)
                {
                    msg += iteration.GetIterationInfo(true);
                }
            }
        }

        return msg;
    }
}
