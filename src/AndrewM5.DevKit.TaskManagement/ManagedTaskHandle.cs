using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.TaskManagement.Abstractions.Interfaces;
using AndrewM5.DevKit.TaskManagement.Services;

namespace AndrewM5.DevKit.TaskManagement;

/// <summary>
/// An internal implementation of <see cref="ITaskHandle"/> that provides a live view 
/// into the state and execution of a managed task.
/// </summary>
internal sealed class ManagedTaskHandle : ITaskHandle
{
    /// <inheritdoc />
    public string TaskKey => _managedTaskRuntime.UserTask.TaskKey;

    /// <inheritdoc />
    public ManagedTaskState State => _managedTaskRuntime.State;

    /// <inheritdoc />
    public Task? RunningTask => _managedTaskRuntime.TaskToRun;


    private readonly ManagedTaskRuntime _managedTaskRuntime;

    /// <summary>
    /// Initializes a new instance of the <see cref="ManagedTaskHandle"/> class.
    /// </summary>
    /// <param name="managedTaskRuntime">The underlying runtime object containing the task's live data.</param>
    public ManagedTaskHandle(ManagedTaskRuntime managedTaskRuntime)
    {
        _managedTaskRuntime = managedTaskRuntime;
    }

    /// <summary>
    /// Retrieves the current runtime duration of the task by querying the global <see cref="TaskManagementHost.TaskManager"/>.
    /// </summary>
    /// <returns>
    /// An <see cref="OperationResult{TimeSpan}"/> containing the elapsed time since the task started, 
    /// or failure details if the manager cannot be reached or the task is not found.
    /// </returns>
    /// <exception cref="Exception">Re-throws the exception encapsulated in the TaskManager's result if the method fails.</exception>
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
