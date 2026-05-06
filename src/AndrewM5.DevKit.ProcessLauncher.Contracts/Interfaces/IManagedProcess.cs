/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

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
    /// <value>A string used to track or look up the process within a manager or collection.</value>
    public string ProcessKey { get; }

    /// <summary>
    /// Gets the underlying <see cref="System.Diagnostics.Process"/> instance. 
    /// </summary>
    /// <value>
    /// The native process object; returns <see langword="null"/> if the process has not been initialized 
    /// or if the instance has been disposed.
    /// </value>
    public Process? Process { get; }

    /// <summary>
    /// Gets the task responsible for monitoring the process lifecycle.
    /// </summary>
    /// <remarks>
    /// This task typically completes when the external process exits or when the monitoring 
    /// logic is cancelled. It can be used to await the process completion asynchronously.
    /// </remarks>
    public Task? MonitorTask { get; }

    /// <summary>
    /// Gets the timestamp of when the process was officially started.
    /// </summary>
    /// <value>A <see cref="DateTime"/> representing the start time in local or UTC time, depending on implementation.</value>
    public DateTime StartTime { get; }

    /// <summary>
    /// Cancels the running process.
    /// </summary>
    /// <param name="forceKill">
    /// If <see langword="true"/>, the process is terminated immediately via <see cref="Process.Kill()"/>; 
    /// otherwise, a graceful shutdown is attempted (e.g., sending a close signal to the main window).
    /// </param>
    /// <returns>
    /// A <see cref="NullOperationResult"/> indicating the outcome of the cancellation request.
    /// </returns>
    public NullOperationResult Cancel(bool forceKill = false);

    /// <summary>
    /// Captures and returns the standard output (STDOUT) produced by the process.
    /// </summary>
    /// <returns>
    /// An <see cref="OperationResult{T}"/> containing the accumulated output string.
    /// </returns>
    public OperationResult<string> GetOutput();

    /// <summary>
    /// Captures and returns the standard error (STDERR) produced by the process.
    /// </summary>
    /// <returns>
    /// An <see cref="OperationResult{T}"/> containing the error output string.
    /// </returns>
    public OperationResult<string> GetError();
}
