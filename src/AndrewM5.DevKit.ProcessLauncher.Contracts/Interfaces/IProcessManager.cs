using AndrewM5.DevKit.Core.Results;

namespace AndrewM5.DevKit.ProcessLauncher.Contracts.Interfaces;

/// <summary>
/// Defines the orchestrator responsible for spawning, tracking, and terminating managed processes.
/// </summary>
public interface IProcessManager
{
    /// <summary>
    /// Initializes and starts a new process based on the provided configuration.
    /// </summary>
    /// <param name="config">The configuration settings for the process, including command, arguments, and monitoring rules.</param>
    /// <returns>
    /// An <see cref="OperationResult{IManagedProcess}"/> containing the managed process instance if successful; 
    /// otherwise, a failure result.
    /// </returns>
    public OperationResult<IManagedProcess> StartProcess(IManagedProcessConfig config);

    /// <summary>
    /// Attempts to cancel and stop a specific process identified by its unique key.
    /// </summary>
    /// <param name="processKey">The unique identifier associated with the process to be cancelled.</param>
    /// <param name="forceKill">
    /// If <see langword="true"/>, the process is terminated immediately; 
    /// otherwise, a graceful shutdown is attempted.
    /// </param>
    /// <returns>A <see cref="NullOperationResult"/> indicating whether the cancellation was successful.</returns>
    public NullOperationResult CancelProcess(string processKey, bool forceKill = false);

    /// <summary>
    /// Attempts to cancel and stop all currently active processes managed by this instance.
    /// </summary>
    /// <param name="forceKill">
    /// If <see langword="true"/>, all processes are terminated immediately; 
    /// otherwise, graceful shutdowns are attempted.
    /// </param>
    /// <returns>A <see cref="NullOperationResult"/> indicating the overall success of the mass cancellation.</returns>
    public NullOperationResult CancelAllProcesses(bool forceKill = false);

    /// <summary>
    /// Checks the current status of a managed process to determine if it is still executing.
    /// </summary>
    /// <param name="processKey">The unique identifier of the process to check.</param>
    /// <returns>
    /// An <see cref="OperationResult{Boolean}"/> where the value is <see langword="true"/> if the process is running; 
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public OperationResult<bool> IsRunning(string processKey);
}
