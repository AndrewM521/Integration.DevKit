using AndrewM5.DevKit.Core.Results;

namespace AndrewM5.DevKit.Threading.Abstractions;

public interface IThreadLockManager
{
    public OperationResult<bool> TryEnterSyncLock(string key, int timeoutMilliseconds = -1);
    
    public OperationResult<bool> TryExitSyncLock(string key);

    public Task<OperationResult<bool>> TryEnterAsyncLock(string key, int timeoutMilliseconds = -1);
    
    public OperationResult<bool> TryExitAsyncLock(string key);
}
