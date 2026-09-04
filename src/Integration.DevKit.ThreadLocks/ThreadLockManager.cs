using Integration.DevKit.Core;
using Integration.DevKit.Core.Logging;
using Integration.DevKit.ThreadLocks.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;

namespace Integration.DevKit.ThreadLocks;

/// <summary>
/// Thread synchronization manager that handles named locks.
/// Supports both synchronous and asynchronous locking mechanisms using unique keys.
/// </summary>
public class ThreadLockManager
{
    /// <summary>
    /// Gets or sets the current runtime settings for this manager, initialized from the bound
    /// <see cref="ThreadLockSettings"/>. Mutate this in place (e.g. <c>RuntimeSettings.EnableLogging = false</c>)
    /// to change behavior, including logging, at runtime.
    /// </summary>
    public ThreadLockSettings RuntimeSettings { get; set; }

    private readonly ConcurrentDictionary<string, ThreadLockInfo_Sync> _syncLocks = new ConcurrentDictionary<string, ThreadLockInfo_Sync>();
    private readonly ConcurrentDictionary<string, ThreadLockInfo_Async> _asyncLocks = new ConcurrentDictionary<string, ThreadLockInfo_Async>();
    private readonly ILogger? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThreadLockManager"/> class.
    /// </summary>
    /// <param name="settings">The initial configuration settings injected via the Options pattern.</param>
    /// <param name="loggerFactory">An optional logger factory to provide diagnostic logging.</param>
    public ThreadLockManager(IOptions<ThreadLockSettings>? settings = null, ILoggerFactory? loggerFactory = null)
    {
        RuntimeSettings = settings?.Value.Clone() ?? new ThreadLockSettings();

        _logger = loggerFactory?.CreateConditionalLogger("ThreadLockManager", () => RuntimeSettings.EnableLogging);
    }

    #region Syncronous Methods
    /// <summary>
    /// Attempts to acquire a synchronous lock associated with the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The unique string identifier for the lock.</param>
    /// <param name="timeoutMilliseconds">
    /// The number of milliseconds to wait for the lock.
    /// Use -1 to wait indefinitely or 0 to test the lock and return immediately.
    /// </param>
    /// <returns>
    /// A <see cref="NullOperationResult"/> representing the outcome.
    /// Success indicates the lock was acquired; Failure indicates a timeout or error.
    /// </returns>
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

                        _logger?.LogWarning($"Timed out waiting for sync lock '{key}' after {timeoutMilliseconds}ms.");

                        return result.SetMethodFailure(new TimeoutException($"Lock timeout for: {key}"));
                    }

                    lockInfo.UpdateLastAccessTime();

                    _logger?.LogDebug($"Acquired sync lock '{key}'.");

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

    /// <summary>
    /// Releases the synchronous lock associated with the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The unique identifier for the lock to release.</param>
    /// <returns>
    /// A <see cref="NullOperationResult"/> indicating whether the release was successful. If the
    /// current thread does not own the lock for the specified key, a failed result wrapping a
    /// <see cref="System.Threading.SynchronizationLockException"/> is returned rather than thrown.
    /// </returns>
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

                _logger?.LogDebug($"Released sync lock '{key}'.");

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
    /// <summary>
    /// Asynchronously attempts to acquire a lock associated with the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The unique identifier for the lock.</param>
    /// <param name="timeoutMilliseconds">
    /// The number of milliseconds to wait for the lock.
    /// Use -1 to wait indefinitely.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains a
    /// <see cref="NullOperationResult"/> indicating whether the lock was successfully acquired.
    /// </returns>
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

                    _logger?.LogWarning($"Timed out waiting for async lock '{key}' after {timeoutMilliseconds}ms.");

                    throw new TimeoutException("Timeout while waiting for async lock");
                }
            }

            lockInfo.UpdateLastAccessTime();

            _logger?.LogDebug($"Acquired async lock '{key}'.");

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Releases the asynchronous lock associated with the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The unique identifier for the lock to release.</param>
    /// <returns>
    /// A <see cref="NullOperationResult"/> indicating whether the release was successful.
    /// </returns>
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

                _logger?.LogDebug($"Released async lock '{key}'.");

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
