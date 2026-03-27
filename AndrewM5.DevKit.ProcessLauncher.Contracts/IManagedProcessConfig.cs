
namespace AndrewM5.DevKit.ProcessLauncher.Abstractions;

public interface IManagedProcessConfig
{
    public string ProcessKey { get; }

    public string Command { get; }

    public string Arguments { get; }

    public bool ShowWindow { get; }

    public string? WorkingDirectory { get; }

    public int TimeoutSeconds { get; }

    public bool EnableProcessLogging { get; }

    public bool AutoRestartOnFailure { get; }
}
