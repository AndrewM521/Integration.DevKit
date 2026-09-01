using Microsoft.Extensions.Logging;

namespace Integration.DevKit.TaskMgmt.Contracts;

/// <summary>
/// Concrete Implementation of <see cref="IIterationStrategy"/>
/// </summary>
public class BaseIterationStrategy : IIterationStrategy
{
    /// <inheritdoc/>
    /// <remarks>
    /// The base implementation returns <see cref="Task.CompletedTask"/> immediately. 
    /// It does not block or await any external triggers, effectively allowing the 
    /// execution engine to start the next task iteration immediately.
    /// </remarks>
    public virtual Task WaitForReadyAsync(IManagedTaskHandle handle, CancellationToken cancellationToken, ILogger? logger = null)
    {
        return Task.CompletedTask;
    }
}
