using AndrewM5.DevKit.Core.Results;
using System.Diagnostics;

namespace AndrewM5.DevKit.ProcessLauncher.Contracts.Interfaces;

public interface IManagedProcess : IAsyncDisposable
{
    public string ProcessKey { get; }

    public Process? Process { get; }

    public Task? MonitorTask { get; }

    public DateTime StartTime { get; }

    public NullOperationResult Start();

    public NullOperationResult Cancel(bool forceKill);
    
    public OperationResult<string> GetOutput();

    public OperationResult<string> GetError();
}
