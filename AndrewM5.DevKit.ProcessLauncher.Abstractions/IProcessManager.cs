using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.ProcessLauncher.Abstractions.Settings;

namespace AndrewM5.DevKit.ProcessLauncher.Abstractions;

public interface IProcessManager
{
    public ProcessManagerSettings RuntimeSettings { get; }

    public OperationResult<IManagedProcess> StartProcess(IManagedProcessConfig config);
    
    public NullOperationResult CancelProcess(string processKey, bool forceKill = false);
    
    public NullOperationResult CancelAllProcesses(bool forceKill = false);
    
    public OperationResult<bool> IsRunning(string processKey);

    public void OutputRuntimeSettings();
}
