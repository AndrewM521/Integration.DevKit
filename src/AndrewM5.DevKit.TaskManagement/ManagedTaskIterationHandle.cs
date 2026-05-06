using AndrewM5.DevKit.TaskManagement.Contracts.Interfaces;

namespace AndrewM5.DevKit.TaskManagement;

/// <summary>
/// Concrete Implementation of <see cref="IManagedTaskIterationHandle"/> providing a public-facing handle for monitoring and controlling a specific iteration of a managed task.
/// </summary>
/// <remarks>
/// This handle allows access to iteration-specific metadata, such as the current <see cref="IterationNumber"/> 
/// and the <see cref="CancellationToken"/> associated with this specific execution cycle.
/// </remarks>
public sealed class ManagedTaskIterationHandle : IManagedTaskIterationHandle
{
    /// <inheritdoc/>
    public int IterationNumber => _taskIterationRuntime.IterationNumber;

    /// <inheritdoc />
    public ManagedTaskState State => _taskIterationRuntime.State;

    /// <inheritdoc/>
    public DateTime StartDTM => _taskIterationRuntime.StartDTM;

    /// <inheritdoc/>
    public DateTime EndDTM => _taskIterationRuntime.EndDTM;

    /// <inheritdoc/>
    public CancellationToken CancelationToken => _taskIterationRuntime.Token;

    /// <inheritdoc/>
    public TimeSpan Runtime => _taskIterationRuntime.Runtime;

    /// <inheritdoc/>
    public bool IsRunning => _taskIterationRuntime.IsRunning;

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public void Cancel() => _taskIterationRuntime.Cancel();
}
