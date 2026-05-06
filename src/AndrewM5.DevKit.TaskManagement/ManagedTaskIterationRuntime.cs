/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using AndrewM5.DevKit.TaskManagement.Contracts.Interfaces;

namespace AndrewM5.DevKit.TaskManagement;

/// <summary>
/// Orchestrates the lifecycle, state, and cancellation logic for a single iteration of a managed task.
/// </summary>
/// <remarks>
/// This class handles the heavy lifting for an iteration, including managing a <see cref="CancellationTokenSource"/> 
/// that links the global task cancellation to the specific iteration context. It is intended for internal 
/// use by the task management engine.
/// </remarks>
internal sealed class ManagedTaskIterationRuntime : IDisposable
{
    private int _state = (int)ManagedTaskState.Idle;
    private readonly CancellationTokenSource _linkedCTS;

    /// <summary>
    /// Gets the public-facing handle associated with this iteration runtime.
    /// </summary>
    public ManagedTaskIterationHandle IterationHandle { get; private set; }

    /// <summary>
    /// Gets the parent task handle that owns this iteration.
    /// </summary>
    public ManagedTaskHandle TaskHandle { get; private set; }

    /// <summary>
    /// Gets the sequence number of this iteration.
    /// </summary>
    public int IterationNumber { get; }

    /// <summary>
    /// Gets or sets the UTC start time of the iteration
    /// </summary>
    /// <value>Defaults to <see cref="DateTime.MinValue"/> if not yet started.</value>
    public DateTime StartDTM { get; internal set; } = DateTime.MinValue;

    /// <summary>
    /// Gets or sets the UTC end time of the iteration.
    /// </summary>
    /// <value> Defaults to <see cref="DateTime.MinValue"/> if still running.</value>
    public DateTime EndDTM { get; internal set; } = DateTime.MinValue;

    /// <summary>
    /// Gets the cancellation token specific to this iteration, linked to the global task token.
    /// </summary>
    public CancellationToken Token => _linkedCTS.Token;

    /// <summary>
    /// Gets the total elapsed time of the iteration. 
    /// If the iteration is currently running, returns the time elapsed since <see cref="StartDTM"/>.
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

    /// <summary>
    /// Gets or sets the current state of the iteration.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="Volatile"/> operations to ensure thread-safe state transitions without full locking overhead.
    /// </remarks>
    public ManagedTaskState State
    {
        get => (ManagedTaskState)Volatile.Read(ref _state);
        internal set => Volatile.Write(ref _state, (int)value);
    }

    /// <summary>
    /// Gets a value indicating whether the iteration is in an active, non-terminal state.
    /// </summary>
    public bool IsRunning { 
        get {

            if (State == ManagedTaskState.Completed || State == ManagedTaskState.Canceled || State == ManagedTaskState.Faulted)
            {
                return false;
            }

            return true;
        } 
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedTaskIterationRuntime"/> class.
    /// </summary>
    /// <param name="taskHandle">The handle of the parent task.</param>
    /// <param name="globalToken">The global token used to link iteration-specific cancellation.</param>
    /// <param name="iterationNumber">The current iteration number.</param>
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

    /// <summary>
    /// Signals cancellation and releases all resources used by the <see cref="ManagedTaskIterationRuntime"/>.
    /// </summary>
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
