using AndrewM5.DevKit.TaskManagement.Abstractions;

namespace AndrewM5.DevKit.TaskManagement.Services;

internal sealed class ManagedTaskRuntime : IDisposable
{
    public IManagedTask UserTask { get; }

    public ManagedTaskState State
    {
        get => (ManagedTaskState)Volatile.Read(ref _state);
        internal set => Volatile.Write(ref _state, (int)value);
    }

    public TaskExecutionMode ExecutionMode { get; }

    internal CancellationTokenSource _cancellationTokenSource { get; set; }
    internal TaskCompletionSource<bool> _completionSource { get; set; }

    public bool ForceCancelRequested { get; internal set; } = false;
    public bool IsLongRunningTask { get; internal set; } = false;

    public Task? TaskToRun { get; internal set; }
    public DateTime StartTime { get; internal set; }
    public DateTime EndTime { get; internal set; }

    private int _state = (int)ManagedTaskState.Idle;

    public ManagedTaskRuntime(IManagedTask task, TaskExecutionMode mode)
    {
        UserTask = task;
        ExecutionMode = mode;

        _cancellationTokenSource = new CancellationTokenSource();
        _completionSource = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
    }

    public void Dispose()
    {
        try
        {
            _cancellationTokenSource?.Cancel();
        }
        catch { }

        _cancellationTokenSource?.Dispose();

        TaskToRun = null;
    }
}