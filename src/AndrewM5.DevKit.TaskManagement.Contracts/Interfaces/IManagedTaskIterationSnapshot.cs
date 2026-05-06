namespace AndrewM5.DevKit.TaskManagement.Contracts.Interfaces;

/// <summary>
/// Defines a read-only snapshot of a managed task's iterations state and execution metrics 
/// at a specific point in time.
/// </summary>
public interface IManagedTaskIterationSnapshot
{
    /// <summary>
    /// Gets the sequence number of this iteration (e.g., 1 for the first loop, 2 for the second).
    /// </summary>
    public int IterationNumber { get; }

    /// <summary>
    /// Gets the final state of this specific iteration.
    /// </summary>
    public ManagedTaskState State { get; }

    /// <summary>
    /// Gets the date and time when this specific iteration began.
    /// </summary>
    public DateTime StartDTM { get; }

    /// <summary>
    /// Gets the date and time when this specific iteration concluded.
    /// </summary>
    public DateTime EndDTM { get; }

    /// <summary>
    /// Gets the total duration spent executing this specific iteration.
    /// </summary>
    public TimeSpan Runtime { get; }

    /// <summary>
    /// Gets the exception that occurred during this specific iteration, if any.
    /// </summary>
    /// <value>
    /// An <see cref="Exception"/> object if the iteration failed; otherwise, <see langword="null"/>.
    /// </value>
    public Exception? Exception { get; }

    /// <summary>
    /// Generates a formatted string summarizing the iteration's metrics.
    /// </summary>
    /// <param name="includeIndent">If <see langword="true"/>, prefixes the string with whitespace for nested display in logs or consoles.</param>
    /// <returns>A human-readable summary of the iteration.</returns>
    public string GetIterationInfo(bool includeIndent = true);
}
