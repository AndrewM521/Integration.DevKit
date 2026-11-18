/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

namespace Integration.DevKit.ProcessLauncher;


/// <summary>
/// Configuration settings required to initialize and manage an external process.
/// </summary>
public class ManagedProcessConfig
{
    /// <summary>
    /// Gets a unique identifier used to track and reference the managed process instance.
    /// </summary>
    /// <value>
    /// A unique string identifier. If not explicitly provided, a new <see cref="Guid"/>
    /// is generated automatically.
    /// </value>
    public string ProcessKey { get; init; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Gets the file path to the executable or the command to be executed.
    /// </summary>
    /// <value>
    /// The executable name or full path. Defaults to <see cref="string.Empty"/>.
    /// </value>
    public string Command { get; init; } = string.Empty;

    /// <summary>
    /// Gets the command-line arguments to be passed to the process at startup.
    /// </summary>
    /// <value>
    /// The string of arguments passed to the executable. Defaults to <see cref="string.Empty"/>.
    /// </value>
    public string Arguments { get; init; } = string.Empty;

    /// <summary>
    /// Gets a value indicating whether a visible window should be created for the process.
    /// </summary>
    /// <value>
    /// <see langword="true"/> to display a window; otherwise <see langword="false"/>.
    /// Defaults to <see langword="false"/> (background execution).
    /// </value>
    public bool ShowWindow { get; init; } = false;

    /// <summary>
    /// Gets the full path to the directory where the process will be executed.
    /// </summary>
    /// <value>
    /// The execution directory. Defaults to <see cref="Environment.CurrentDirectory"/>.
    /// </value>
    public string? WorkingDirectory { get; init; } = Environment.CurrentDirectory;

    /// <summary>
    /// Gets the maximum duration, in seconds, allowed for the process to run before being considered timed out.
    /// </summary>
    /// <value>
    /// Total seconds allowed for execution. Defaults to -1 (no timeout).
    /// </value>
    /// <remarks>
    /// Depending on implementation, exceeding this duration may trigger an automatic
    /// <see cref="ManagedProcess.Cancel(bool)"/> call.
    /// </remarks>
    public int TimeoutSeconds { get; set; } = -1;

    /// <summary>
    /// Gets the maximum number of lines kept for each of standard output and standard error.
    /// </summary>
    /// <value>
    /// Defaults to 10,000. When the limit is reached the oldest lines are dropped.
    /// Zero or negative keeps every line, which grows without limit for long-running, chatty processes.
    /// </value>
    public int MaxOutputLines { get; set; } = 10_000;
}
