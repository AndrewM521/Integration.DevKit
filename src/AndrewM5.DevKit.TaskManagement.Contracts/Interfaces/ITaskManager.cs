using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.TaskManagement.Contracts.Models;
using AndrewM5.DevKit.TaskManagement.Contracts.Options;

namespace AndrewM5.DevKit.TaskManagement.Contracts.Interfaces;

/// <summary>
/// Defines the core contract for a manager responsible for the lifecycle, 
/// execution, and monitoring of managed tasks.
/// </summary>
public interface ITaskManager
{
    /// <summary>
    /// Gets the current active configuration and limits for the task manager.
    /// </summary>
    public TaskManagerSettings RuntimeSettings { get; }

    /// <summary>
    /// Initiates a task and tracks it within the manager.
    /// </summary>
    /// <param name="managedTask">The task implementation to be executed.</param>
    /// <param name="executionMode">Specifies whether the task should run synchronously or asynchronously.</param>
    /// <param name="settings">Execution-specific settings for this task instance.</param>
    /// <param name="cancellationToken">An external token to monitor for cancellation requests.</param>
    /// <returns>An <see cref="OperationResult{ITaskHandle}"/> containing the handle to the started task if successful.</returns>
    public Task<OperationResult<IManagedTaskHandle>> StartTask(ManagedTask managedTask, TaskExecutionMode executionMode, ManagedTaskSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests the cancellation of a specific task.
    /// </summary>
    /// <param name="taskKey">The unique identifier of the task to cancel.</param>
    /// <param name="forceCancel">If true, attempts an immediate termination; otherwise, requests a graceful shutdown.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating success or failure of the cancellation request.</returns>
    public NullOperationResult CancelTask(string taskKey, bool forceCancel = false);

    /// <summary>
    /// Requests the cancellation of all currently tracked tasks.
    /// </summary>
    /// <param name="forceCancel">If true, attempts an immediate termination of all tasks; otherwise, requests a graceful shutdown.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating the overall outcome of the bulk cancellation.</returns>
    public NullOperationResult CancelAllTasks(bool forceCancel = false);

    /// <summary>
    /// Checks whether a task associated with the specified key is currently in a running state.
    /// </summary>
    /// <param name="taskKey">The unique identifier of the task.</param>
    /// <returns>An <see cref="OperationResult{Boolean}"/> where the value is true if the task is active.</returns>
    public OperationResult<bool> IsTaskRunning(string taskKey);

    /// <summary>
    /// Retrieves a collection of keys for all tasks currently managed and running.
    /// </summary>
    /// <returns>An enumerable of task key strings.</returns>
    public IEnumerable<string> GetAllRunningTaskKeys();

    /// <summary>
    /// Returns a task that completes when all provided tasks in the list have finished execution.
    /// </summary>
    /// <param name="tasksList">A list of <see cref="Task"/> objects to monitor.</param>
    /// <returns>A <see cref="Task"/> representing the completion of all input tasks.</returns>
    public Task AwaitAllTasksToFinish(List<Task> tasksList);

    /// <summary>
    /// Logs or outputs the current <see cref="RuntimeSettings"/> for diagnostic purposes.
    /// </summary>
    public void OutputRuntimeSettings();
}

/// <summary>
/// Defines the threading behavior for a task.
/// </summary>
public enum TaskExecutionMode
{
    /// <summary>
    /// The task runs on a background thread, allowing the caller to continue immediately.
    /// </summary>
    Asyncronous,
    /// <summary>
    /// The task runs on the current thread, blocking the caller until completion.
    /// </summary>
    Syncronous
}

/// <summary>
/// Represents the various lifecycle stages of a managed task.
/// </summary>
public enum ManagedTaskState
{
    /// <summary> The task has been initialized but not yet started. </summary>
    Idle,
    /// <summary> The task is currently undergoing initialization and setup. </summary>
    Starting,
    /// <summary> The task is actively executing its workload. </summary>
    Running,
    /// <summary> The task has finished successfully. </summary>
    Completed,
    /// <summary> The task was stopped by a user or system request. </summary>
    Canceled,
    /// <summary> The task stopped due to an unhandled exception or error. </summary>
    Faulted,
    /// <summary> A cancellation has been signaled, but the task has not yet stopped. </summary>
    CancelRequested
}