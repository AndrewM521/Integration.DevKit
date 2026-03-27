using AndrewM5.DevKit.TaskManagement.Abstractions.Interfaces;
using AndrewM5.DevKit.TaskManagement.Abstractions.Models;
using System.Threading;

namespace AndrewM5.DevKit.TaskManagement;

internal sealed class ManagedTaskRuntime : IDisposable
{
    public TaskExecutionMode ExecutionMode { get; }
    public IManagedTask UserTask { get; }
    public ManagedTaskState State
    {
        get => (ManagedTaskState)Volatile.Read(ref _state);
        internal set => Volatile.Write(ref _state, (int)value);
    }
    public int IterationCount
    {
        get => Volatile.Read(ref _iterationCount);
    }

    public ManagedTaskSettings RuntimeSettings { get; }
    public Task? TaskToRun { get; internal set; }
    public DateTime StartTime { get; internal set; }
    public DateTime EndTime { get; internal set; }

    internal CancellationTokenSource _lifecycleCTS;
    internal CancellationTokenSource _iterationCTS;
    internal readonly CancellationToken _externalCT;

    private int _state = (int)ManagedTaskState.Idle;
    private int _iterationCount;
    

    public ManagedTaskRuntime(IManagedTask task, ManagedTaskSettings settings, CancellationToken cancellationToken = default)
    {
        UserTask = task;
        RuntimeSettings = settings;

        _externalCT = cancellationToken;

        _lifecycleCTS = new CancellationTokenSource();
        _iterationCTS = new CancellationTokenSource();
    }

    internal void IncrementIteration()
    {
        Interlocked.Increment(ref _iterationCount);
    }

    internal void ResetIterationToken()
    {
        _iterationCTS?.Dispose();
        _iterationCTS = new CancellationTokenSource();
    }

    public void Dispose()
    {
        try
        {
            _lifecycleCTS?.Cancel();
        }
        catch { }

        _lifecycleCTS?.Dispose();

        TaskToRun = null;
    }
}