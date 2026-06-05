/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.TaskMgmt.Contracts;

namespace Integration.DevKit.TaskMgmt;

/// <summary>
/// The internal controller responsible for the lifecycle, concurrency, and state management of a managed task.
/// </summary>
/// <remarks>
/// This class acts as the central coordinator. It maintains the master <see cref="CancellationTokenSource"/>,
/// manages concurrency via a <see cref="SemaphoreSlim"/>, and tracks the total number of iterations executed.
/// </remarks>
internal sealed class ManagedTaskRuntime : IDisposable
{
    private int _state = (int)ManagedTaskState.Idle;
    private int _iterationCount;

    /// <summary>
    /// Gets the public-facing handle used to monitor and control this task runtime.
    /// </summary>
    public ManagedTaskHandle Handle { get; private set; }

    /// <summary>
    /// Gets the configuration settings defining how this task should be executed.
    /// </summary>
    public ManagedTaskSettings RuntimeSettings { get; }

    /// <summary>
    /// Gets or sets the underlying <see cref="Task"/> representing the asynchronous execution of the task's lifecycle.
    /// </summary>
    public Task? LifecycleTask { get; internal set; }

    /// <summary>
    /// Gets the user-defined task definition and logic.
    /// </summary>
    public ManagedTask UserTask { get; }

    /// <summary>
    /// Gets or sets the high-level state of the task. 
    /// </summary>
    /// <remarks>
    /// Updated using <see cref="Volatile"/> to ensure thread-safe visibility across threads.
    /// </remarks>
    public ManagedTaskState State
    {
        get => (ManagedTaskState)Volatile.Read(ref _state);
        internal set => Volatile.Write(ref _state, (int)value);
    }

    /// <summary>
    /// Gets the total number of iterations that have been initiated by this runtime.
    /// </summary>
    public int IterationCount
    {
        get => Volatile.Read(ref _iterationCount);
    }

    /// <summary>
    /// Gets or sets the UTC start time of the task lifecycle.
    /// </summary>
    /// <value>Defaults to <see cref="DateTime.MinValue"/> if not yet started.</value>
    public DateTime StartDTM { get; internal set; } = DateTime.MinValue;

    /// <summary>
    /// Gets or sets the UTC end time of the task lifecycle.
    /// </summary>
    /// <value> Defaults to <see cref="DateTime.MinValue"/> if still running.</value>
    public DateTime EndDTM { get; internal set; } = DateTime.MinValue;

    /// <summary>
    /// Gets the total active runtime of the task. 
    /// If still running, calculates the duration from <see cref="StartDTM"/> to the current UTC time.
    /// </summary>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedTaskRuntime"/> class.
    /// </summary>
    /// <param name="task">The task definition to execute.</param>
    /// <param name="settings">The runtime settings, including concurrency limits.</param>
    /// <param name="externalCancelationToken">An optional external token to link to the task's lifecycle.</param>
    internal ManagedTaskRuntime(ManagedTask task, ManagedTaskSettings settings, CancellationToken externalCancelationToken = default)
    {
        UserTask = task;
        RuntimeSettings = settings;

        _lifecycleCTS = CancellationTokenSource.CreateLinkedTokenSource(externalCancelationToken);
        _concurrencyLock = new SemaphoreSlim(settings.MaxConcurrentParallelTasks);

        Handle = new ManagedTaskHandle(this);
    }

    /// <summary>
    /// Increments the iteration counter and creates a new <see cref="ManagedTaskIterationRuntime"/>.
    /// </summary>
    /// <returns>A new iteration runtime context linked to this task's lifecycle and token.</returns>
    internal ManagedTaskIterationRuntime CreateIterationRuntime()
    {
        // Increment global counter
        int nextId = Interlocked.Increment(ref _iterationCount);

        // Create a new runtime for this next iteration
        return new ManagedTaskIterationRuntime(Handle, _lifecycleCTS.Token, nextId);
    }

    /// <summary>
    /// Signals a cancellation request for the entire task lifecycle and all associated iterations.
    /// </summary>
    public void Cancel()
    {
        State = ManagedTaskState.CancelRequested;

        if (_lifecycleCTS != null && !_lifecycleCTS.IsCancellationRequested)
        {
            _lifecycleCTS.Cancel();
        }
    }

    /// <summary>
    /// Disposes the underlying cancellation sources and concurrency locks.
    /// </summary>
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