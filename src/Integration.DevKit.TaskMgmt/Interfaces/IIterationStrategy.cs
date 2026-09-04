/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.Logging;

namespace Integration.DevKit.TaskMgmt.Interfaces;

/// <summary>
/// Defines the contract for controlling the timing and flow of managed task iterations.
/// </summary>
/// <remarks>
/// Implementations of this interface act as a gatekeeper between task iterations. 
/// The execution engine will await <see cref="WaitForReadyAsync"/> before each 
/// iteration begins, allowing for strategies like fixed delays, scheduling, or back-off logic.
/// </remarks>
public interface IIterationStrategy
{
    /// <summary>
    /// Asynchronously waits until the task is ready to proceed to its next iteration.
    /// </summary>
    /// <param name="handle">The handle to the current task runtime, used to inspect metrics or state to make timing decisions.</param>
    /// <param name="cancellationToken">A cancellation token that is triggered if the task or service is stopped.</param>
    /// <param name="logger">An optional <see cref="ILogger"/> for recording diagnostic information regarding the wait period.</param>
    /// <returns>
    /// A <see cref="Task"/> that represents the waiting period. The task completes
    /// only when the engine is cleared to execute the next iteration.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown if the <paramref name="cancellationToken"/> is signaled during the wait.
    /// </exception>
    public Task WaitForReadyAsync(IManagedTaskHandle handle, CancellationToken cancellationToken, ILogger? logger = null);
}
