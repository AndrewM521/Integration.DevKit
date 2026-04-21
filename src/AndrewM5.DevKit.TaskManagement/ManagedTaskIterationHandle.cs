using AndrewM5.DevKit.TaskManagement.Contracts.Interfaces;

namespace AndrewM5.DevKit.TaskManagement;

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
    public CancellationToken Token => _taskIterationRuntime.Token;

    /// <inheritdoc/>
    public TimeSpan Runtime => _taskIterationRuntime.Runtime;

    /// <inheritdoc/>
    public bool IsRunning => _taskIterationRuntime.IsRunning;

    /// <inheritdoc/>
    public IManagedTaskHandle TaskHandle => _taskIterationRuntime.TaskHandle;

    private readonly ManagedTaskIterationRuntime _taskIterationRuntime;

    internal ManagedTaskIterationHandle(ManagedTaskIterationRuntime runtime)
    {
        _taskIterationRuntime = runtime;
    }

    /// <inheritdoc/>
    public void Cancel() => _taskIterationRuntime.Cancel();
}
