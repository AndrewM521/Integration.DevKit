using AndrewM5.DevKit.Logging.Abstractions;
using AndrewM5.DevKit.TaskManagement.Abstractions;
using Microsoft.Extensions.Logging;

namespace AndrewM5.DevKit.TaskManagement.Services;

public sealed class ManagedTaskSnapshot : IManagedTaskSnapshot
{
    public required string TaskKey { get; init; }
    public ManagedTaskState State { get; init; }
    public DateTime StartUtc { get; init; }
    public DateTime EndUtc { get; init; }
    public TimeSpan Runtime {
        get {
            if (StartUtc == DateTime.MinValue)
            {
                return TimeSpan.Zero;
            }

            return ((EndUtc == DateTime.MinValue ? DateTime.UtcNow : EndUtc) - StartUtc);
        }
    }
    public string? ErrorMessage { get; init; }
    public string? ErrorType { get; init; }

    public static ManagedTaskSnapshot From(
        string taskKey,
        ManagedTaskState state,
        DateTime startUtc,
        DateTime endUtc,
        Exception? ex = null)
    {
        return new ManagedTaskSnapshot
        {
            TaskKey = taskKey,
            State = state,
            StartUtc = startUtc,
            EndUtc = endUtc,
            ErrorMessage = ex?.Message,
            ErrorType = ex?.GetType().FullName
        };
    }

    public void DisplaySnapshot(ICustomLogger? logger = null)
    {
        string msg = @$"
            TaskKey: {TaskKey}
            State: {State}
            StartUtc: {StartUtc}
            EndUtc: {EndUtc}
            Runtime: {Runtime}
            ErrorMessage: {ErrorMessage}
            ErrorType: {ErrorType}
        ";

        logger?.LogDebug(msg);
    }
}
