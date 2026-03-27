using AndrewM5.DevKit.Core.Results;

namespace AndrewM5.DevKit.ProcessLauncher.Contracts.Interfaces;

public interface IProcessManager
{
    public OperationResult<IManagedProcess> StartProcess(IManagedProcessConfig config);
    
    public NullOperationResult CancelProcess(string processKey, bool forceKill = false);
    
    public NullOperationResult CancelAllProcesses(bool forceKill = false);
    
    public OperationResult<bool> IsRunning(string processKey);
}
