using AndrewM5.DevKit.Core.Results;
using System.Diagnostics;

namespace AndrewM5.DevKit.ProcessLauncher.Contracts.Interfaces;

/// <summary>
/// Defines the contract for a managed process wrapper, providing mechanisms to start, 
/// monitor, and terminate an external system process.
/// </summary>
public interface IManagedProcess : IAsyncDisposable
{
    /// <summary>
    /// Gets a unique identifier associated with this managed process instance.
    /// </summary>
    public string ProcessKey { get; }

    /// <summary>
    /// Gets the underlying <see cref="System.Diagnostics.Process"/> instance. 
    /// Returns <see langword="null"/> if the process has not been initialized.
    /// </summary>
    public Process? Process { get; }

    /// <summary>
    /// Gets the task responsible for monitoring the process lifecycle (e.g., waiting for exit).
    /// </summary>
    public Task? MonitorTask { get; }

    /// <summary>
    /// Gets the timestamp of when the process was officially started.
    /// </summary>
    public DateTime StartTime { get; }

    /// <summary>
    /// Attempts to start the process.
    /// </summary>
    /// <returns>A <see cref="NullOperationResult"/> indicating whether the process started successfully.</returns>
    public NullOperationResult Start();

    /// <summary>
    /// Cancels the running process.
    /// </summary>
    /// <param name="forceKill">
    /// If <see langword="true"/>, the process is terminated immediately (SIGKILL); 
    /// otherwise, a graceful shutdown is attempted.
    /// </param>
    /// <returns>A <see cref="NullOperationResult"/> indicating the outcome of the cancellation request.</returns>
    public NullOperationResult Cancel(bool forceKill);

    /// <summary>
    /// Captures and returns the standard output (STDOUT) produced by the process.
    /// </summary>
    /// <returns>An <see cref="OperationResult{T}"/> containing the output string.</returns>
    public OperationResult<string> GetOutput();

    /// <summary>
    /// Captures and returns the standard error (STDERR) produced by the process.
    /// </summary>
    /// <returns>An <see cref="OperationResult{T}"/> containing the error string.</returns>
    public OperationResult<string> GetError();
}
