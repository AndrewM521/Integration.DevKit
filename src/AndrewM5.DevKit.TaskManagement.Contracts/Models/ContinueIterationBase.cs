using AndrewM5.DevKit.Logging.Contracts.Interfaces;
using AndrewM5.DevKit.TaskManagement.Contracts.Interfaces;

namespace AndrewM5.DevKit.TaskManagement.Contracts.Models;

/// <summary>
/// Provides a default, non-blocking implementation for task iteration control.
/// </summary>
/// <remarks>
/// This class serves as the base for all iteration strategies. By default, it does not 
/// introduce any delay, allowing the task execution engine to proceed to the next 
/// iteration immediately.
/// </remarks>
public class ContinueIterationBase : IContinueIteration
{
    /// <inheritdoc/>
    /// <remarks>
    /// Default implementation: Returns immediately to start the next iteration without delay.
    /// </remarks>
    public virtual Task WaitForReadyAsync(ITaskHandle handle, CancellationToken cancellationToken, ICustomLogger? logger = null)
    {
        return Task.CompletedTask;
    }
}
