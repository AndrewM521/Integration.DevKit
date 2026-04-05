using AndrewM5.DevKit.Logging.Contracts.Interfaces;
using AndrewM5.DevKit.TaskManagement.Abstractions.Interfaces;
using AndrewM5.DevKit.TaskManagement.Abstractions.Models;
using Microsoft.Extensions.Logging;

namespace AndrewM5.DevKit.TaskManagement;

/// <summary>
/// A concrete implementation of <see cref="IManagedTaskSnapshot"/> providing a 
/// thread-safe, read-only view of a task's metrics and state.
/// </summary>
public sealed class ManagedTaskSnapshot : IManagedTaskSnapshot
{
    /// <inheritdoc/>
    public string TaskKey { get; init; } = "";

    /// <summary>
    /// Gets the configuration settings applied to the task at the time of the snapshot.
    /// </summary>
    public ManagedTaskSettings? Settings { get; init; }

    /// <inheritdoc/>
    public ManagedTaskState State { get; internal set; }

    /// <inheritdoc/>
    public int IterationCount { get; internal set; }

    /// <inheritdoc/>
    public DateTime StartTime { get; internal set; }

    /// <inheritdoc/>
    public DateTime EndTime { get; internal set; }

    /// <summary>
    /// Gets the calculated duration of the task. 
    /// If the task is still active, it returns the duration from <see cref="StartTime"/> to current UTC time.
    /// </summary>
    public TimeSpan Runtime {
        get {
            if (StartTime == DateTime.MinValue)
            {
                return TimeSpan.Zero;
            }

            return (EndTime == DateTime.MinValue ? DateTime.UtcNow : EndTime) - StartTime;
        }
    }

    /// <inheritdoc/>
    public Exception? Exception { get; internal set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedTaskSnapshot"/> class.
    /// </summary>
    /// <param name="taskKey">The unique identifier for the task.</param>
    /// <param name="settings">The settings the task is running with.</param>
    public ManagedTaskSnapshot(string taskKey, ManagedTaskSettings settings)
    {
        TaskKey = taskKey;
        Settings = settings;
    }

    /// <summary>
    /// Formats and logs the current snapshot data to the provided <see cref="ICustomLogger"/>.
    /// </summary>
    /// <param name="logger">The logger instance to receive the debug output. If null, no action is taken.</param>
    public void DisplaySnapshot(ICustomLogger? logger = null)
    {
        string msg = @$"
            TaskKey: {TaskKey}
            State: {State}
            IterationCount: {IterationCount}
            StartUtc: {StartTime}
            EndUtc: {EndTime}
            Runtime: {Runtime}
            ExceptionType: {Exception?.GetType()}
            ExceptionMessage: {Exception?.Message}
        ";

        logger?.LogDebug(msg);
    }
}
