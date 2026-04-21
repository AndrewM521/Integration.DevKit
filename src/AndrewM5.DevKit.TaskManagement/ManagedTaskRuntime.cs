using AndrewM5.DevKit.TaskManagement.Contracts.Interfaces;
using AndrewM5.DevKit.TaskManagement.Contracts.Models;

namespace AndrewM5.DevKit.TaskManagement;

internal sealed class ManagedTaskRuntime : IDisposable
{
    public ManagedTaskHandle Handle { get; private set; }

    public ManagedTaskSettings RuntimeSettings { get; }

    public Task? LifecycleTask { get; internal set; }

    public ManagedTask UserTask { get; }

    private int _state = (int)ManagedTaskState.Idle;
    public ManagedTaskState State
    {
        get => (ManagedTaskState)Volatile.Read(ref _state);
        internal set => Volatile.Write(ref _state, (int)value);
    }

    private int _iterationCount;
    public int IterationCount
    {
        get => Volatile.Read(ref _iterationCount);
    }

    public DateTime StartDTM { get; internal set; } = DateTime.MinValue;

    public DateTime EndDTM { get; internal set; } = DateTime.MinValue;

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

    internal SemaphoreSlim _concurrencyLock;
    internal CancellationTokenSource _lifecycleCTS;

    internal ManagedTaskRuntime(ManagedTask task, ManagedTaskSettings settings, CancellationToken externalCancelationToken = default)
    {
        UserTask = task;
        RuntimeSettings = settings;

        _lifecycleCTS = CancellationTokenSource.CreateLinkedTokenSource(externalCancelationToken);
        _concurrencyLock = new SemaphoreSlim(settings.MaxConcurrentParallelTasks);

        Handle = new ManagedTaskHandle(this);
    }

    internal ManagedTaskIterationRuntime CreateIterationRuntime()
    {
        // Increment global counter
        int nextId = Interlocked.Increment(ref _iterationCount);

        // Create a new runtime for this next iteration
        return new ManagedTaskIterationRuntime(Handle, _lifecycleCTS.Token, nextId);
    }

    public void Cancel()
    {
        State = ManagedTaskState.CancelRequested;

        if (_lifecycleCTS != null && !_lifecycleCTS.IsCancellationRequested)
        {
            _lifecycleCTS.Cancel();
        }
    }

    public void Dispose()
    {
        // Signal cancellation to the lifecycle
        // This will automatically trigger cancellation for ALL active IterationHandles
        // because their tokens are linked to this one.
        try
        {
            _lifecycleCTS?.Cancel();

            // 2. Dispose the Token Sources
            _lifecycleCTS?.Dispose();
        }
        catch (ObjectDisposedException) { /* Already gone */ }

        try
        {
            // 3. Dispose the Concurrency Lock
            // This is important! Semaphores use wait handles that should be released.
            _concurrencyLock?.Dispose();
        }
        catch (ObjectDisposedException) { /* Already gone */ }
    }
}