namespace AndrewM5.DevKit.ProcessLauncher.Contracts.Interfaces;

/// <summary>
/// Defines the configuration settings required to initialize and manage an external process.
/// </summary>
public interface IManagedProcessConfig
{
    /// <summary>
    /// Gets a unique identifier used to track and reference the managed process instance.
    /// </summary>
    public string ProcessKey { get; }

    /// <summary>
    /// Gets the file path to the executable or the command to be executed.
    /// </summary>
    public string Command { get; }

    /// <summary>
    /// Gets the command-line arguments to be passed to the process at startup.
    /// </summary>
    public string Arguments { get; }

    /// <summary>
    /// Gets a value indicating whether a visible window should be created for the process.
    /// </summary>
    public bool ShowWindow { get; }

    /// <summary>
    /// Gets the full path to the directory where the process will be executed. 
    /// If <see langword="null"/>, the current application's working directory is used.
    /// </summary>
    public string? WorkingDirectory { get; }

    /// <summary>
    /// Gets the maximum duration, in seconds, allowed for the process to run before being considered timed out.
    /// </summary>
    public int TimeoutSeconds { get; }

    /// <summary>
    /// Gets a value indicating whether standard output and error streams should be captured and logged.
    /// </summary>
    public bool EnableProcessLogging { get; }

    /// <summary>
    /// Gets a value indicating whether the manager should attempt to restart the process automatically if it exits unexpectedly.
    /// </summary>
    public bool AutoRestartOnFailure { get; }
}
