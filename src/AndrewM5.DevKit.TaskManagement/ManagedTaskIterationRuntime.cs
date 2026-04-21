using AndrewM5.DevKit.TaskManagement.Contracts.Interfaces;

namespace AndrewM5.DevKit.TaskManagement;

internal sealed class ManagedTaskIterationRuntime : IDisposable
{
    public ManagedTaskIterationHandle IterationHandle { get; private set; }

    public ManagedTaskHandle TaskHandle { get; private set; }

    public int IterationNumber { get; }

    public DateTime StartDTM { get; internal set; } = DateTime.MinValue;

    public DateTime EndDTM { get; internal set; } = DateTime.MinValue;

    public CancellationToken Token => _linkedCTS.Token;

    public TimeSpan Runtime
    {
        get
        {
            if (StartDTM == DateTime.MinValue)
            {
                return TimeSpan.Zero;
            }

            return (EndDTM == DateTime.MinValue ? DateTime.UtcNow : EndDTM) - StartDTM;
        }
    }

    private int _state = (int)ManagedTaskState.Idle;
    public ManagedTaskState State
    {
        get => (ManagedTaskState)Volatile.Read(ref _state);
        internal set => Volatile.Write(ref _state, (int)value);
    }

    public bool IsRunning { 
        get {

            if (State == ManagedTaskState.Completed || State == ManagedTaskState.Canceled || State == ManagedTaskState.Faulted)
            {
                return false;
            }

            return true;
        } 
    }

    private readonly CancellationTokenSource _linkedCTS;

    internal ManagedTaskIterationRuntime(ManagedTaskHandle taskHandle, CancellationToken globalToken, int iterationNumber)
    {
        TaskHandle = taskHandle;
        _linkedCTS = CancellationTokenSource.CreateLinkedTokenSource(globalToken);
        
        IterationNumber = iterationNumber;
        IterationHandle = new ManagedTaskIterationHandle(this);
    }

    public void Cancel()
    {
        State = ManagedTaskState.CancelRequested;

        if (_linkedCTS != null && !_linkedCTS.IsCancellationRequested)
        {
            _linkedCTS.Cancel();
        }
    }

    public void Dispose()
    {
        // Signal cancellation to the lifecycle
        // This will automatically trigger cancellation for ALL active IterationHandles
        // because their tokens are linked to this one.
        try
        {
            _linkedCTS?.Cancel();

            // 2. Dispose the Token Sources
            _linkedCTS?.Dispose();
        }
        catch (ObjectDisposedException) { /* Already gone */ }
    }
}
