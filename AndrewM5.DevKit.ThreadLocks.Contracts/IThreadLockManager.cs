using AndrewM5.DevKit.Core.Results;

namespace AndrewM5.DevKit.ThreadLocks.Abstractions;

public interface IThreadLockManager
{
    public NullOperationResult TryEnterSyncLock(string key, int timeoutMilliseconds = -1);
    
    public NullOperationResult TryExitSyncLock(string key);

    public Task<NullOperationResult> TryEnterAsyncLock(string key, int timeoutMilliseconds = -1);
    
    public NullOperationResult TryExitAsyncLock(string key);
}
