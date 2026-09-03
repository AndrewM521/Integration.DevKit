/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.ProcessLauncher.Contracts;

namespace Integration.DevKit.ProcessLauncher;


/// <summary>
/// Concrete Implementation of <see cref="IManagedProcessConfig"/>
/// </summary>
public class ManagedProcessConfig : IManagedProcessConfig
{
    /// <inheritdoc />
    /// <value>
    /// A unique string identifier. If not explicitly provided, a new <see cref="Guid"/> 
    /// is generated automatically.
    /// </value>
    public string ProcessKey { get; init; } = Guid.NewGuid().ToString();

    /// <inheritdoc />
    /// <value>
    /// The executable name or full path. Defaults to <see cref="string.Empty"/>.
    /// </value>
    public string Command { get; init; } = string.Empty;

    /// <inheritdoc />
    /// <value>
    /// The string of arguments passed to the executable. Defaults to <see cref="string.Empty"/>.
    /// </value>
    public string Arguments { get; init; } = string.Empty;

    /// <inheritdoc />
    /// <value>
    /// <see langword="true"/> to display a window; otherwise <see langword="false"/>. 
    /// Defaults to <see langword="false"/> (background execution).
    /// </value>
    public bool ShowWindow { get; init; } = false;

    /// <inheritdoc />
    /// <value>
    /// The execution directory. Defaults to <see cref="Environment.CurrentDirectory"/>.
    /// </value>
    public string? WorkingDirectory { get; init; } = Environment.CurrentDirectory;

    /// <inheritdoc />
    /// <value>
    /// Total seconds allowed for execution. Defaults to -1 (no timeout).
    /// </value>
    public int TimeoutSeconds { get; set; } = -1;

    /// <inheritdoc />
    /// <value>
    /// Enables standard stream capture. Defaults to <see langword="true"/>.
    /// </value>
    public bool EnableProcessLogging { get; set; } = true;

    /// <inheritdoc />
    /// <value>
    /// Reserved for future use; currently has no effect on process behavior. Defaults to <see langword="false"/>.
    /// </value>
    public bool AutoRestartOnFailure { get; set; } = false;
}
