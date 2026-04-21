
using AndrewM5.DevKit.Logging.Contracts.Interfaces;

namespace AndrewM5.DevKit.TaskManagement.Contracts.Interfaces;

/// <summary>
/// Defines the contract for controlling the timing and flow of managed task iterations.
/// </summary>
/// <remarks>
/// Implementations of this interface act as a gatekeeper between task iterations. 
/// The execution engine will await <see cref="WaitForReadyAsync"/> before each 
/// iteration begins
/// </remarks>
public interface IIterationStrategy
{
    /// <summary>
    /// Asynchronously waits until the task is ready to proceed to its next iteration.
    /// </summary>
    /// <param name="handle">A handle to the current task runtime if needed to make timing decisions.</param>
    /// <param name="cancellationToken">A cancellation token that is triggered if the task or service is stopped. </param>
    /// <returns>
    /// A <see cref="Task"/> that represents the waiting period. The task should 
    /// complete only when the engine is cleared to execute the next iteration.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    /// Thrown if the <paramref name="cancellationToken"/> is signaled during the wait.
    /// </exception>
    public Task WaitForReadyAsync(IManagedTaskHandle handle, CancellationToken cancellationToken, ICustomLogger? logger = null);
}
