using AndrewM5.DevKit.Core.Results;

namespace AndrewM5.DevKit.TaskManagement.Abstractions;

public interface ITaskHandle
{
    public string TaskKey { get; }
    public ManagedTaskState State { get; }
    public Task? RunningTask { get; }

    public OperationResult<TimeSpan> GetTaskRuntime();
}
