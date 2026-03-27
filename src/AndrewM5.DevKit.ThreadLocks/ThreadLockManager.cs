using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.ThreadLocks.Contracts.Interfaces;
using System.Collections.Concurrent;

namespace AndrewM5.DevKit.ThreadLocks;

public class ThreadLockManager : IThreadLockManager
{
    private readonly ConcurrentDictionary<string, ThreadLockInfo_Sync> _syncLocks = new ConcurrentDictionary<string, ThreadLockInfo_Sync>();
    private readonly ConcurrentDictionary<string, ThreadLockInfo_Async> _asyncLocks = new ConcurrentDictionary<string, ThreadLockInfo_Async>();

    public ThreadLockManager() {}

    #region Syncronous Methods
    public NullOperationResult TryEnterSyncLock(string key, int timeoutMilliseconds = -1)
    {
        var result = new NullOperationResult();

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

            lockInfo.UpdateLastAccessTime();

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    public NullOperationResult TryExitSyncLock(string key)
    {
        var result = new NullOperationResult();

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

                lockInfo.UpdateLastAccessTime();

                if (Interlocked.Decrement(ref lockInfo.RefCount) <= 0)
                {
                    _syncLocks.TryRemove(key, out _);
                }
            }

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }
    #endregion

    #region Asyncronous Methods
    public async Task<NullOperationResult> TryEnterAsyncLock(string key, int timeoutMilliseconds = -1)
    {
        var result = new NullOperationResult();

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

            lockInfo.UpdateLastAccessTime();

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    public NullOperationResult TryExitAsyncLock(string key)
    {
        var result = new NullOperationResult();

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

                lockInfo.UpdateLastAccessTime();

                if (Interlocked.Decrement(ref lockInfo.RefCount) <= 0)
                {
                    if (_asyncLocks.TryRemove(new KeyValuePair<string, ThreadLockInfo_Async>(key, lockInfo)))
                    {
                        lockInfo.Dispose();
                    }
                }
            }

            return result.SetMethodSuccess();
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
