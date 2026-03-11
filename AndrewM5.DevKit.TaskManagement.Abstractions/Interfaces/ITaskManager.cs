using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.TaskManagement.Abstractions.Models;
using AndrewM5.DevKit.TaskManagement.Abstractions.Settings;

namespace AndrewM5.DevKit.TaskManagement.Abstractions.Interfaces;

public interface ITaskManager
{
    public TaskManagerSettings RuntimeSettings { get; }

    public Task<OperationResult<ITaskHandle>> StartTask(IManagedTask managedTask, ManagedTaskSettings settings, CancellationToken cancellationToken = default);

    public NullOperationResult CancelTask(string taskKey, bool forceCancel = false);
    
    public NullOperationResult CancelAllTasks(bool forceCancel = false);

    public OperationResult<bool> IsTaskRunning(string taskKey);
    
    public OperationResult<TimeSpan> GetTaskRuntime(string taskKey);

    public IEnumerable<string> GetAllRunningTaskKeys();
    
    public Task AwaitAllTasksToFinish(List<Task> tasksList);

    public void OutputRuntimeSettings();
}

public enum TaskExecutionMode
{
    Asyncronous,
    Syncronous
}

public enum ManagedTaskState
{
    Idle,
    Starting,
    Running,
    Completed,
    Canceled,
    Faulted,
    CancelRequested
}