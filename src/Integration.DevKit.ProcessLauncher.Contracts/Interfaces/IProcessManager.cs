using System.Diagnostics;
using Integration.DevKit.Core;

namespace Integration.DevKit.ProcessLauncher.Contracts;

/// <summary>
/// Defines the orchestrator responsible for spawning, tracking, and terminating managed processes.
/// </summary>
/// <remarks>
/// This manager maintains an internal registry of <see cref="IManagedProcess"/> instances, 
/// keyed by their <see cref="IManagedProcessConfig.ProcessKey"/>. It acts as a single point of 
/// control for aggregate operations and process lookups.
/// </remarks>
public interface IProcessManager
{
    /// <summary>
    /// Gets or sets the current runtime settings for this manager, initialized from the bound
    /// <see cref="ProcessLauncherSettings"/>. Mutate this in place (e.g. <c>RuntimeSettings.EnableLogging = false</c>)
    /// to change behavior, including logging, at runtime.
    /// </summary>
    public ProcessLauncherSettings RuntimeSettings { get; set; }

    /// <summary>
    /// Initializes and starts a new process based on the provided configuration.
    /// </summary>
    /// <param name="config">The configuration settings for the process, including command, arguments, and monitoring rules.</param>
    /// <returns>
    /// An <see cref="OperationResult{IManagedProcess}"/> containing the managed process instance if successful; 
    /// otherwise, a failure result containing error details.
    /// </returns>
    public OperationResult<IManagedProcess> StartProcess(IManagedProcessConfig config);

    /// <summary>
    /// Attempts to cancel and stop a specific process identified by its unique key.
    /// </summary>
    /// <param name="processKey">The unique identifier associated with the process to be cancelled.</param>
    /// <param name="forceKill">
    /// If <see langword="true"/>, the process is terminated immediately (SIGKILL); 
    /// otherwise, a graceful shutdown is attempted. Defaults to <see langword="false"/>.
    /// </param>
    /// <returns>
    /// A <see cref="NullOperationResult"/> indicating whether the cancellation was successful. 
    /// Returns a failure if the <paramref name="processKey"/> is not found.
    /// </returns>
    public NullOperationResult CancelProcess(string processKey, bool forceKill = false);

    /// <summary>
    /// Attempts to cancel and stop all currently active processes managed by this instance.
    /// </summary>
    /// <param name="forceKill">
    /// If <see langword="true"/>, all processes are terminated immediately; 
    /// otherwise, graceful shutdowns are attempted. Defaults to <see langword="false"/>.
    /// </param>
    /// <returns>
    /// A <see cref="NullOperationResult"/> indicating the overall success of the mass cancellation.
    /// </returns>
    /// <remarks>
    /// In the event of a partial failure (some processes stopped while others failed to terminate), 
    /// the returned result should aggregate these errors.
    /// </remarks>
    public NullOperationResult CancelAllProcesses(bool forceKill = false);

    /// <summary>
    /// Checks the current status of a managed process to determine if it is still executing.
    /// </summary>
    /// <param name="processKey">The unique identifier of the process to check.</param>
    /// <returns>
    /// An <see cref="OperationResult{Boolean}"/> where the value is <see langword="true"/> if the process is 
    /// found and currently running; otherwise, <see langword="false"/>.
    /// </returns>
    public OperationResult<bool> IsRunning(string processKey);

    /// <summary>
    /// Periodically polls the process status until it exits or the cancellation token is triggered.
    /// </summary>
    /// <param name="process">The process to monitor.</param>
    /// <param name="token">A token to signal abandonment of the wait operation.</param>
    public Task WaitForExitAsync(Process process, CancellationToken token = default);
}
