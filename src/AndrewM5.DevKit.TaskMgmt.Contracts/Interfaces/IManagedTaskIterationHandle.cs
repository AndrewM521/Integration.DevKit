/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

namespace AndrewM5.DevKit.TaskMgmt.Contracts.Interfaces;

/// <summary>
/// Provides access to the context and telemetry of a specific task iteration during its execution.
/// </summary>
/// <remarks>
/// Unlike a snapshot, a handle provides a "live" link to the iteration, allowing for 
/// real-time monitoring of runtime and the ability to trigger iteration-specific cancellation.
/// </remarks>
public interface IManagedTaskIterationHandle
{
    /// <summary>
    /// Gets the handle of the parent task that owns this iteration.
    /// </summary>
    public IManagedTaskHandle TaskHandle { get; }

    /// <summary>
    /// Gets the unique sequence number of this iteration for the current task run.
    /// </summary>
    public int IterationNumber { get; }

    /// <summary>
    /// Gets the UTC timestamp of when this specific iteration began.
    /// </summary>
    public DateTime StartDTM { get; }

    /// <summary>
    /// Gets the cancellation token for this iteration.
    /// </summary>
    /// <remarks>
    /// This token is a linked token; it will signal cancellation if either 
    /// this specific iteration is cancelled via <see cref="Cancel"/> or if the 
    /// parent <see cref="TaskHandle"/> is cancelled.
    /// </remarks>
    public CancellationToken CancelationToken { get; }

    /// <summary>
    /// Gets the calculated duration of this iteration. 
    /// </summary>
    public TimeSpan Runtime { get; }

    /// <summary>
    /// Gets a value indicating whether this specific iteration is currently executing.
    /// </summary>
    public bool IsRunning { get; }

    /// <summary>
    /// Requests cancellation for this specific iteration only.
    /// </summary>
    /// <remarks>
    /// Calling this method triggers the <see cref="CancelationToken"/>, allowing the current iteration 
    /// to exit gracefully without necessarily terminating the entire parent task.
    /// </remarks>
    public void Cancel();
}
