using Integration.DevKit.Core;

namespace Integration.DevKit.TaskMgmt.Contracts;

/// <summary>
/// Defines the core contract for a manager responsible for the lifecycle, 
/// execution, and monitoring of managed tasks.
/// </summary>
public interface ITaskManager
{
    /// <summary>
    /// Gets the current active configuration and limits for the task manager.
    /// </summary>
    /// <value>A <see cref="TaskManagerSettings"/> instance containing concurrency limits and timeout values.</value>
    public TaskManagerSettings RuntimeSettings { get; }

    /// <summary>
    /// Re-derives everything this manager caches from <see cref="RuntimeSettings"/> (currently the
    /// concurrent-task rate limiter). Call this after mutating <see cref="RuntimeSettings"/> in place
    /// so the change takes effect.
    /// </summary>
    /// <returns>A <see cref="NullOperationResult"/> indicating success or failure.</returns>
    public NullOperationResult Initialize();

    /// <summary>
    /// Initiates a task and tracks it within the manager.
    /// </summary>
    /// <param name="managedTask">The task implementation to be executed.</param>
    /// <param name="executionMode">Specifies whether the task should run synchronously or asynchronously.</param>
    /// <param name="settings">Execution-specific settings for this task instance.</param>
    /// <param name="cancellationToken">An external token to monitor for cancellation requests.</param>
    /// <returns>
    /// An <see cref="OperationResult{ITaskHandle}"/> containing the handle to the started task if successful; 
    /// otherwise, a failure result explaining why the task could not be initiated.
    /// </returns>
    public Task<OperationResult<IManagedTaskHandle>> StartTask(ManagedTask managedTask, TaskExecutionMode executionMode, ManagedTaskSettings settings, CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests the cancellation of a specific task.
    /// </summary>
    /// <param name="taskKey">The unique identifier of the task to cancel.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating success or failure of the cancellation request.</returns>
    public NullOperationResult CancelTask(string taskKey);

    /// <summary>
    /// Requests the cancellation of all currently tracked tasks.
    /// </summary>
    /// <returns>A <see cref="NullOperationResult"/> indicating the overall outcome of the bulk cancellation.</returns>
    public NullOperationResult CancelAllTasks();

    /// <summary>
    /// Checks whether a task associated with the specified key is currently in a running state.
    /// </summary>
    /// <param name="taskKey">The unique identifier of the task.</param>
    /// <returns>
    /// An <see cref="OperationResult{Boolean}"/> where the value is <see langword="true"/> if the task exists and is active.
    /// </returns>
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
    /// Logging method to output current <see cref="TaskManagerSettings"/> to the logs.
    /// </summary>
    public void LogRuntimeSettings();
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