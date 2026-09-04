/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.TaskMgmt.Interfaces;

namespace Integration.DevKit.TaskMgmt;

/// <summary>
/// Provides access to the context and telemetry of a specific task iteration during its execution,
/// as a public-facing handle for monitoring and controlling that iteration.
/// </summary>
/// <remarks>
/// Unlike a snapshot, a handle provides a "live" link to the iteration, allowing for
/// real-time monitoring of runtime and the ability to trigger iteration-specific cancellation.
/// This handle allows access to iteration-specific metadata, such as the current <see cref="IterationNumber"/>
/// and the <see cref="CancellationToken"/> associated with this specific execution cycle.
/// </remarks>
public sealed class ManagedTaskIterationHandle
{
    /// <summary>
    /// Gets the sequence number for this iteration.
    /// </summary>
    public int IterationNumber => _taskIterationRuntime.IterationNumber;

    /// <summary>
    /// Gets the current state of the iteration.
    /// </summary>
    public ManagedTaskState State => _taskIterationRuntime.State;

    /// <summary>
    /// Gets the UTC start time for this iteration.
    /// </summary>
    public DateTime StartDTM => _taskIterationRuntime.StartDTM;

    /// <summary>
    /// Gets the UTC end time for this iteration, if it has completed.
    /// </summary>
    public DateTime EndDTM => _taskIterationRuntime.EndDTM;

    /// <summary>
    /// Gets the cancellation token associated with this iteration.
    /// </summary>
    public CancellationToken CancelationToken => _taskIterationRuntime.Token;

    /// <summary>
    /// Gets the total runtime of this iteration.
    /// </summary>
    public TimeSpan Runtime => _taskIterationRuntime.Runtime;

    /// <summary>
    /// Gets a value indicating whether this iteration is currently running.
    /// </summary>
    public bool IsRunning => _taskIterationRuntime.IsRunning;

    /// <summary>
    /// Gets the parent task handle that owns this iteration.
    /// </summary>
    public IManagedTaskHandle TaskHandle => _taskIterationRuntime.TaskHandle;

    private readonly ManagedTaskIterationRuntime _taskIterationRuntime;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedTaskIterationHandle"/> class.
    /// </summary>
    /// <param name="runtime">The internal runtime instance for the current iteration.</param>
    internal ManagedTaskIterationHandle(ManagedTaskIterationRuntime runtime)
    {
        _taskIterationRuntime = runtime;
    }

    /// <summary>
    /// Requests cancellation for this specific iteration only.
    /// </summary>
    /// <remarks>
    /// Calling this method triggers the <see cref="CancelationToken"/>, allowing the current iteration
    /// to exit gracefully without necessarily terminating the entire parent task.
    /// </remarks>
    public void Cancel() => _taskIterationRuntime.Cancel();
}
