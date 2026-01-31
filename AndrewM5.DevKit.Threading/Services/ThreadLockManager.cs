using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.Threading.Abstractions;
using AndrewM5.DevKit.Threading.Utilities;
using System.Collections.Concurrent;

namespace AndrewM5.DevKit.Threading.Services;

public class ThreadLockManager : IThreadLockManager
{
    private readonly ConcurrentDictionary<string, ThreadLockInfo_Sync> _syncLocks = new ConcurrentDictionary<string, ThreadLockInfo_Sync>();
    private readonly ConcurrentDictionary<string, ThreadLockInfo_Async> _asyncLocks = new ConcurrentDictionary<string, ThreadLockInfo_Async>();

    public ThreadLockManager() {}

    #region Syncronous Methods
    public OperationResult<bool> TryEnterSyncLock(string key, int timeoutMilliseconds = -1)
    {
        var result = new OperationResult<bool>();

        try
        {
            ValidateAndNormalizeKey(ref key);

            if (timeoutMilliseconds < -1)
            {
                timeoutMilliseconds = -1;
            }

            var lockInfo = _syncLocks.GetOrAdd(key, _ => new ThreadLockInfo_Sync());

            Interlocked.Increment(ref lockInfo.RefCount);

            if (!Monitor.TryEnter(lockInfo.LockObject, timeoutMilliseconds))
            {
                Interlocked.Decrement(ref lockInfo.RefCount);
                throw new TimeoutException("Timeout while waiting for lock.");
            }

            return result.SetMethodSuccess(true);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    public OperationResult<bool> TryExitSyncLock(string key)
    {
        var result = new OperationResult<bool>();

        try
        {
            ValidateAndNormalizeKey(ref key);

            if (_syncLocks.TryGetValue(key, out var lockInfo))
            {
                if (!Monitor.IsEntered(lockInfo.LockObject))
                {
                    throw new SynchronizationLockException($"Current thread does not own sync lock '{key}'.");
                }

                Monitor.Exit(lockInfo.LockObject);

                if (Interlocked.Decrement(ref lockInfo.RefCount) <= 0)
                {
                    _syncLocks.TryRemove(key, out _);
                }
            }

            return result.SetMethodSuccess(true);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }
    #endregion

    #region Asyncronous Methods
    public async Task<OperationResult<bool>> TryEnterAsyncLock(string key, int timeoutMilliseconds = -1)
    {
        var result = new OperationResult<bool>();

        try
        {
            ValidateAndNormalizeKey(ref key);

            var lockInfo = _asyncLocks.GetOrAdd(key, _ => new ThreadLockInfo_Async());

            Interlocked.Increment(ref lockInfo.RefCount);

            if (timeoutMilliseconds < 0)
            {
                await lockInfo.Semaphore.WaitAsync().ConfigureAwait(false);
            }
            else
            {
                bool entered = await lockInfo.Semaphore.WaitAsync(timeoutMilliseconds).ConfigureAwait(false);

                if (!entered)
                {
                    Interlocked.Decrement(ref lockInfo.RefCount);
                    throw new TimeoutException("Timeout while waiting for async lock");
                }
            }

            return result.SetMethodSuccess(true);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    public OperationResult<bool> TryExitAsyncLock(string key)
    {
        var result = new OperationResult<bool>();

        try
        {
            ValidateAndNormalizeKey(ref key);

            if (_asyncLocks.TryGetValue(key, out var lockInfo))
            {
                try
                {
                    lockInfo.Semaphore.Release();
                }
                catch (SemaphoreFullException ex)
                {
                    throw new InvalidOperationException($"Async lock '{key}' was released more times than it was acquired.", ex);
                }

                if (Interlocked.Decrement(ref lockInfo.RefCount) <= 0)
                {
                    if (_asyncLocks.TryRemove(new KeyValuePair<string, ThreadLockInfo_Async>(key, lockInfo)))
                    {
                        lockInfo.Dispose();
                    }
                }
            }

            return result.SetMethodSuccess(true);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }
    #endregion

    private static void ValidateAndNormalizeKey(ref string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or white space");
        }

        key = key.Trim().ToLowerInvariant();
    }
}
