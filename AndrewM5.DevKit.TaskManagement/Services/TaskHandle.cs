using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.TaskManagement.Abstractions;

namespace AndrewM5.DevKit.TaskManagement.Services;

internal sealed class TaskHandle : ITaskHandle
{
    public string TaskKey => _managedTaskRuntime.UserTask.TaskKey;
    public ManagedTaskState State => _managedTaskRuntime.State;
    public Task? RunningTask => _managedTaskRuntime.TaskToRun;


    private readonly ManagedTaskRuntime _managedTaskRuntime;

    public TaskHandle(ManagedTaskRuntime managedTaskRuntime)
    {
        _managedTaskRuntime = managedTaskRuntime;
    }

    public OperationResult<TimeSpan> GetTaskRuntime()
    {
        var result = new OperationResult<TimeSpan>();

        try
        {
            var getRunTime = TaskManagementHost.TaskManager!.GetTaskRuntime(TaskKey);
            if (!getRunTime.MethodSuccess)
            {
                throw getRunTime.Exception;
            }

            return result.SetMethodSuccess(getRunTime.Result);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }
}
