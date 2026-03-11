using AndrewM5.DevKit.ProcessLauncher.Abstractions;

namespace AndrewM5.DevKit.ProcessLauncher;

public class ManagedProcessConfig : IManagedProcessConfig
{
    public string ProcessKey { get; init; } = Guid.NewGuid().ToString();

    public string Command { get; init; } = string.Empty;

    public string Arguments { get; init; } = string.Empty;

    public bool ShowWindow { get; init; } = false;

    public string? WorkingDirectory { get; init; } = Environment.CurrentDirectory;

    public int TimeoutSeconds { get; set; } = -1;

    public bool EnableProcessLogging { get; set; } = true;

    public bool AutoRestartOnFailure { get; set; } = false;
}
