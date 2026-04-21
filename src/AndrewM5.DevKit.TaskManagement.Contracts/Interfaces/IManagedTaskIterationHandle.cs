
namespace AndrewM5.DevKit.TaskManagement.Contracts.Interfaces;

/// <summary>
/// Provides access to the context and telemetry of a specific task iteration.
/// </summary>
public interface IManagedTaskIterationHandle
{
    public IManagedTaskHandle TaskHandle { get; }

    /// <summary>
    /// Gets the unique sequence number of this iteration for the current task run.
    /// </summary>
    public int IterationNumber { get; }

    /// <summary>
    /// Gets the UTC timestamp of when this specific iteration began.
    /// </summary>
    public DateTime StartDTM { get; }

    /// <summary>
    /// Gets the cancellation token for this iteration. This is linked with the parent task token 
    /// </summary>
    public CancellationToken Token { get; }

    /// <summary>
    /// Gets the calculated duration of this iteration. 
    /// If the iteration is still active, it returns the duration from <see cref="StartDTM"/> to current UTC time.
    /// </summary>
    public TimeSpan Runtime { get; }

    public bool IsRunning { get; }

    /// <summary>
    /// Requests cancellation for this specific iteration only.
    /// </summary>
    public void Cancel();
}
