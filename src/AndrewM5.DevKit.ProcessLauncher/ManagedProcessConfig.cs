using AndrewM5.DevKit.ProcessLauncher.Contracts.Interfaces;

namespace AndrewM5.DevKit.ProcessLauncher;

/// <summary>
/// A concrete implementation of <see cref="IManagedProcessConfig"/> used to define 
/// the startup parameters and behavior for a managed process.
/// </summary>
public class ManagedProcessConfig : IManagedProcessConfig
{
    /// <inheritdoc />
    /// <value>Defaults to a new <see cref="Guid"/> string if not specified.</value>
    public string ProcessKey { get; init; } = Guid.NewGuid().ToString();

    /// <inheritdoc />
    /// <value>Defaults to <see cref="string.Empty"/> if not specified.</value>
    public string Command { get; init; } = string.Empty;

    /// <inheritdoc />
    /// <value>Defaults to <see cref="string.Empty"/> if not specified.</value>
    public string Arguments { get; init; } = string.Empty;

    /// <inheritdoc />
    /// <value>Defaults to <see langword="false"/> (background/hidden process).</value>
    public bool ShowWindow { get; init; } = false;

    /// <inheritdoc />
    /// <value>Defaults to the current application's working directory (<see cref="Environment.CurrentDirectory"/>).</value>
    public string? WorkingDirectory { get; init; } = Environment.CurrentDirectory;

    /// <inheritdoc />
    /// <value>Defaults to -1 (infinite/no timeout).</value>
    public int TimeoutSeconds { get; set; } = -1;

    /// <inheritdoc />
    /// <value>Defaults to <see langword="true"/>.</value>
    public bool EnableProcessLogging { get; set; } = true;

    /// <inheritdoc />
    /// <value>Defaults to <see langword="false"/>.</value>
    public bool AutoRestartOnFailure { get; set; } = false;
}
