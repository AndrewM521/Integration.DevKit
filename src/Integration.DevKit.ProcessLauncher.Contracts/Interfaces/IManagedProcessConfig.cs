/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

namespace Integration.DevKit.ProcessLauncher.Contracts;

/// <summary>
/// Defines the configuration settings required to initialize and manage an external process.
/// </summary>
public interface IManagedProcessConfig
{
    /// <summary>
    /// Gets a unique identifier used to track and reference the managed process instance.
    /// </summary>
    /// <value>A unique string key.</value>
    public string ProcessKey { get; }

    /// <summary>
    /// Gets the file path to the executable or the command to be executed.
    /// </summary>
    /// <value>The absolute path to an .exe or a command available in the system PATH.</value>
    public string Command { get; }

    /// <summary>
    /// Gets the command-line arguments to be passed to the process at startup.
    /// </summary>
    /// <value>A string containing all arguments, properly escaped if necessary.</value>
    public string Arguments { get; }

    /// <summary>
    /// Gets a value indicating whether a visible window should be created for the process.
    /// </summary>
    /// <value>
    /// <see langword="true"/> to show the process window; <see langword="false"/> to run the 
    /// process in the background (headless).
    /// </value>
    public bool ShowWindow { get; }

    /// <summary>
    /// Gets the full path to the directory where the process will be executed. 
    /// </summary>
    /// <value>
    /// The directory path, or <see langword="null"/> to inherit the current application's 
    /// working directory.
    /// </value>
    public string? WorkingDirectory { get; }

    /// <summary>
    /// Gets the maximum duration, in seconds, allowed for the process to run before being considered timed out.
    /// </summary>
    /// <value>The timeout in seconds. A value of 0 or -1 indicates no timeout.</value>
    /// <remarks>
    /// Depending on implementation, exceeding this duration may trigger an automatic 
    /// <see cref="IManagedProcess.Cancel(bool)"/> call.
    /// </remarks>
    public int TimeoutSeconds { get; }

    /// <summary>
    /// Gets a value indicating whether standard output and error streams should be captured and logged.
    /// </summary>
    /// <remarks>
    /// When enabled, the process manager will hook into the STDOUT and STDERR streams, 
    /// allowing the data to be retrieved via <see cref="IManagedProcess.GetOutput"/>.
    /// </remarks>
    public bool EnableProcessLogging { get; }

    /// <summary>
    /// Reserved for future use; currently has no effect on process behavior — there is no auto-restart
    /// logic implemented anywhere in this module. Don't rely on it.
    /// </summary>
    public bool AutoRestartOnFailure { get; }
}
