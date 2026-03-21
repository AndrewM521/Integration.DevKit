using AndrewM5.DevKit.Logging.Abstractions;
using AndrewM5.DevKit.TaskManagement.Abstractions.Interfaces;
using AndrewM5.DevKit.TaskManagement.Abstractions.Models;
using Microsoft.Extensions.Logging;

namespace AndrewM5.DevKit.TaskManagement;

public sealed class ManagedTaskSnapshot : IManagedTaskSnapshot
{
    public string TaskKey { get; init; } = "";
    public ManagedTaskSettings? Settings { get; init; }

    public ManagedTaskState State { get; internal set; }
    public int IterationCount { get; internal set; }
    public DateTime StartTime { get; internal set; }
    public DateTime EndTime { get; internal set; }
    public TimeSpan Runtime {
        get {
            if (StartTime == DateTime.MinValue)
            {
                return TimeSpan.Zero;
            }

            return (EndTime == DateTime.MinValue ? DateTime.UtcNow : EndTime) - StartTime;
        }
    }
    public Exception? Exception { get; internal set; }

    public ManagedTaskSnapshot(string taskKey, ManagedTaskSettings settings)
    {
        TaskKey = taskKey;
        Settings = settings;
    }

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
