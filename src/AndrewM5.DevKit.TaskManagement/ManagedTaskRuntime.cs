using AndrewM5.DevKit.TaskManagement.Abstractions.Interfaces;
using AndrewM5.DevKit.TaskManagement.Abstractions.Models;
using System.Threading;

namespace AndrewM5.DevKit.TaskManagement;

/// <summary>
/// Handles the execution lifecycle, state management, and cancellation logic for an <see cref="IManagedTask"/>.
/// </summary>
internal sealed class ManagedTaskRuntime : IDisposable
{
    /// <summary>
    /// Gets the execution strategy (e.g., FireAndForget, LongRunning) for the task.
    /// </summary>
    public TaskExecutionMode ExecutionMode { get; }

    /// <summary>
    /// Gets the underlying user-defined task implementation.
    /// </summary>
    public IManagedTask UserTask { get; }

    /// <summary>
    /// Gets or sets the current execution state of the task. 
    /// Managed via thread-safe volatile operations.
    /// </summary>
    public ManagedTaskState State
    {
        get => (ManagedTaskState)Volatile.Read(ref _state);
        internal set => Volatile.Write(ref _state, (int)value);
    }

    /// <summary>
    /// Gets the total number of iterations completed by the task.
    /// </summary>
    public int IterationCount
    {
        get => Volatile.Read(ref _iterationCount);
    }

    /// <summary>
    /// Gets the configuration settings used to initialize this runtime.
    /// </summary>
    public ManagedTaskSettings RuntimeSettings { get; }

    /// <summary>
    /// Gets or sets the actual asynchronous <see cref="Task"/> being executed.
    /// </summary>
    public Task? TaskToRun { get; internal set; }

    /// <summary>
    /// Gets or sets the UTC timestamp of when the task execution started.
    /// </summary>
    public DateTime StartTime { get; internal set; }

    /// <summary>
    /// Gets or sets the UTC timestamp of when the task execution reached a terminal state.
    /// </summary>
    public DateTime EndTime { get; internal set; }

    /// <summary>
    /// Managed token source for the entire lifespan of the task runtime.
    /// </summary>
    internal CancellationTokenSource _lifecycleCTS;

    /// <summary>
    /// Managed token source specifically for the current execution iteration.
    /// </summary>
    internal CancellationTokenSource _iterationCTS;

    /// <summary>
    /// The cancellation token passed from the external caller or host.
    /// </summary>
    internal readonly CancellationToken _externalCT;

    private int _state = (int)ManagedTaskState.Idle;
    private int _iterationCount;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedTaskRuntime"/> class.
    /// </summary>
    /// <param name="task">The task implementation to run.</param>
    /// <param name="settings">Configuration for execution behavior.</param>
    /// <param name="cancellationToken">An optional external token to observe for cancellation.</param>
    public ManagedTaskRuntime(IManagedTask task, ManagedTaskSettings settings, CancellationToken cancellationToken = default)
    {
        UserTask = task;
        RuntimeSettings = settings;

        _externalCT = cancellationToken;

        _lifecycleCTS = new CancellationTokenSource();
        _iterationCTS = new CancellationTokenSource();
    }

    /// <summary>
    /// Atomically increments the iteration counter.
    /// </summary>
    internal void IncrementIteration()
    {
        Interlocked.Increment(ref _iterationCount);
    }

    /// <summary>
    /// Disposes of the current iteration token and initializes a new one for the next run.
    /// </summary>
    internal void ResetIterationToken()
    {
        _iterationCTS?.Dispose();
        _iterationCTS = new CancellationTokenSource();
    }

    /// <summary>
    /// Cancels the lifecycle token and releases resources used by the runtime.
    /// </summary>
    public void Dispose()
    {
        try
        {
            _lifecycleCTS?.Cancel();
        }
        catch { }

        _lifecycleCTS?.Dispose();
        _iterationCTS?.Dispose();

        TaskToRun = null;
    }
}