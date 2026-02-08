using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.ProcessLauncher.Abstractions.Settings;

namespace AndrewM5.DevKit.ProcessLauncher.Abstractions;

public interface IProcessManager
{
    public ProcessManagerSettings RuntimeSettings { get; }

    public OperationResult<IManagedProcess> StartProcess(IManagedProcessConfig config);
    
    public OperationResult<bool> CancelProcess(string processKey, bool forceKill = false);
    
    public OperationResult<bool> CancelAllProcesses(bool forceKill = false);
    
    public OperationResult<bool> IsRunning(string processKey);

    public void OutputRuntimeSettings();
}
