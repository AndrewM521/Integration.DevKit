namespace AndrewM5.DevKit.Threading.Services;

public sealed class ManagedTaskSnapshot
{
    public required string TaskKey { get; init; }
    
    public ManagedTaskState State { get; init; }

    public DateTime StartUtc { get; init; }
    public DateTime EndUtc { get; init; }

    public TimeSpan Runtime =>
        StartUtc == DateTime.MinValue
            ? TimeSpan.Zero
            : ((EndUtc == DateTime.MinValue ? DateTime.UtcNow : EndUtc) - StartUtc);

    public string? ErrorMessage { get; init; }
    public string? ErrorType { get; init; }

    public static ManagedTaskSnapshot From(ManagedTask task, Exception? ex = null)
    {
        return new ManagedTaskSnapshot
        {
            TaskKey = task.TaskKey,
            State = task.State,
            StartUtc = task._taskStartTime,
            EndUtc = task._taskEndTime,
            ErrorMessage = ex?.Message,
            ErrorType = ex?.GetType().FullName
        };
    }
}
