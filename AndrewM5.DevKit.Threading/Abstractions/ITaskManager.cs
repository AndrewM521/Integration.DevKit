using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.Threading.Abstractions.Settings;
using AndrewM5.DevKit.Threading.Services;

namespace AndrewM5.DevKit.Threading.Abstractions;
public interface ITaskManager
{
    public TaskManagerSettings RuntimeSettings { get; }

    public Task<OperationResult<ManagedTask>> StartTask(ManagedTask managedTask, TaskExecutionMode mode);
    
    public OperationResult<bool> CancelTask(string taskKey, bool forceCancel = false);
    
    public OperationResult<bool> CancelAllTasks(bool forceCancel = false);

    public Task<OperationResult<bool>> RestartTask(ManagedTask managedTask);

    public OperationResult<bool> IsTaskRunning(string taskKey);
    
    public OperationResult<TimeSpan> GetTaskRuntime(string taskKey);

    public IEnumerable<string> GetAllRunningTaskKeys();
    
    public Task AwaitAllTasksToFinish(List<Task> tasksList);

    public void OutputRuntimeSettings();
}
