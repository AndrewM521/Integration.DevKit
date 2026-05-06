/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.ThreadLocks.Contracts.Interfaces;
using System.Collections.Concurrent;

namespace AndrewM5.DevKit.ThreadLocks;

/// <summary>
/// Concrete Implementation of <see cref="IThreadLockManager"/>
/// </summary>
public class ThreadLockManager : IThreadLockManager
{
    private readonly ConcurrentDictionary<string, ThreadLockInfo_Sync> _syncLocks = new ConcurrentDictionary<string, ThreadLockInfo_Sync>();
    private readonly ConcurrentDictionary<string, ThreadLockInfo_Async> _asyncLocks = new ConcurrentDictionary<string, ThreadLockInfo_Async>();

    #region Syncronous Methods
    /// <inheritdoc />
    /// <remarks>
    /// Uses <see cref="Monitor"/> for the locking mechanism. Increments the reference count 
    /// before attempting to enter to prevent premature cleanup.
    /// </remarks>
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

            while (true) // Loop to handle the race condition
            {
                var lockInfo = _syncLocks.GetOrAdd(key, _ => new ThreadLockInfo_Sync());

                // Use a temporary increment to "signal" interest
                Interlocked.Increment(ref lockInfo.RefCount);

                // Verify this is still the active object in the map
                if (_syncLocks.TryGetValue(key, out var current) && ReferenceEquals(current, lockInfo))
                {
                    // ACTUALLY attempt the Monitor lock
                    if (!Monitor.TryEnter(lockInfo.LockObject, timeoutMilliseconds))
                    {
                        // We failed to get the lock (timeout)
                        Interlocked.Decrement(ref lockInfo.RefCount);
                        return result.SetMethodFailure(new TimeoutException($"Lock timeout for: {key}"));
                    }

                    lockInfo.UpdateLastAccessTime();
                    return result.SetMethodSuccess();
                }
                else
                {
                    // If we get here, the lockInfo was removed/replaced by another thread 
                    // while we were trying to increment it. Back out and loop.
                    Interlocked.Decrement(ref lockInfo.RefCount);
                }
            }

            //var lockInfo = _syncLocks.GetOrAdd(key, _ => new ThreadLockInfo_Sync());

            //Interlocked.Increment(ref lockInfo.RefCount);

            //if (!Monitor.TryEnter(lockInfo.LockObject, timeoutMilliseconds))
            //{
            //    Interlocked.Decrement(ref lockInfo.RefCount);
            //    throw new TimeoutException("Timeout while waiting for lock.");
            //}

            //lockInfo.UpdateLastAccessTime();

            //return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// Decrements the reference count. If the count reaches zero, the key is removed 
    /// from the internal dictionary to free memory.
    /// </remarks>
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
                    _syncLocks.TryRemove(new KeyValuePair<string, ThreadLockInfo_Sync>(key, lockInfo));
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
    /// <inheritdoc />
    /// <remarks>
    /// Uses <see cref="SemaphoreSlim"/> to provide non-blocking waits. Increments the reference 
    /// count before the awaitable operation.
    /// </remarks>
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

    /// <inheritdoc />
    /// <remarks>
    /// Releases the semaphore and decrements the reference count. If the count reaches zero, 
    /// the semaphore is disposed and removed from the dictionary.
    /// </remarks>
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

    /// <summary>
    /// Validates that the key is not null or whitespace and normalizes it to a 
    /// trimmed, lowercase invariant format for consistent dictionary lookups.
    /// </summary>
    /// <param name="key">The key string to validate and transform by reference.</param>
    /// <exception cref="ArgumentException">Thrown if the key is null or white space.</exception>
    private static void ValidateAndNormalizeKey(ref string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be null or white space");
        }

        key = key.Trim().ToLowerInvariant();
    }
}
