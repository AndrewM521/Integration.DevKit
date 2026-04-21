using AndrewM5.DevKit.TaskManagement.Contracts.Interfaces;

namespace AndrewM5.DevKit.TaskManagement;

public sealed class ManagedTaskHandle : IManagedTaskHandle
{
    /// <inheritdoc />
    public string TaskKey => _managedTaskRuntime.UserTask.TaskKey;

    /// <inheritdoc />
    public ManagedTaskState State => _managedTaskRuntime.State;

    /// <inheritdoc />
    public Task? RunningTask => _managedTaskRuntime.LifecycleTask;

    /// <inheritdoc />
    public int CurrentIterationCount => _managedTaskRuntime.IterationCount;

    /// <inheritdoc />
    public DateTime StartDTM => _managedTaskRuntime.StartDTM;

    /// <inheritdoc />
    public DateTime EndDTM => _managedTaskRuntime.EndDTM;

    /// <inheritdoc/>
    public TimeSpan Runtime => _managedTaskRuntime.Runtime;

    private readonly ManagedTaskRuntime _managedTaskRuntime;

    internal ManagedTaskHandle(ManagedTaskRuntime managedTaskRuntime)
    {
        _managedTaskRuntime = managedTaskRuntime;
    }

    /// <inheritdoc/>
    public void Cancel() => _managedTaskRuntime.Cancel();
}
