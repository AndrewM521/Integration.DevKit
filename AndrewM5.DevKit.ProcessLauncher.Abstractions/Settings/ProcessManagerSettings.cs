namespace AndrewM5.DevKit.ProcessLauncher.Abstractions.Settings;

public class ProcessManagerSettings
{
    public int MaxProcessesCount { get; set; } = int.MaxValue;

    public ProcessManagerSettings Clone()
    {
        return new ProcessManagerSettings
        {
            MaxProcessesCount = this.MaxProcessesCount,
        };
    }
}
