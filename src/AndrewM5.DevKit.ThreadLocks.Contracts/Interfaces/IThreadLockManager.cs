using AndrewM5.DevKit.Core.Results;

namespace AndrewM5.DevKit.ThreadLocks.Contracts.Interfaces;

/// <summary>
/// Defines a contract for a thread synchronization manager that handles named locks.
/// This interface supports both synchronous and asynchronous locking mechanisms using unique keys.
/// </summary>
public interface IThreadLockManager
{
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
    public NullOperationResult TryEnterSyncLock(string key, int timeoutMilliseconds = -1);

    /// <summary>
    /// Releases the synchronous lock associated with the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The unique identifier for the lock to release.</param>
    /// <returns>
    /// A <see cref="NullOperationResult"/> indicating whether the release was successful.
    /// </returns>
    /// <exception cref="System.Threading.SynchronizationLockException">
    /// Thrown if the current thread does not own the lock for the specified key.
    /// </exception>
    public NullOperationResult TryExitSyncLock(string key);

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
    public Task<NullOperationResult> TryEnterAsyncLock(string key, int timeoutMilliseconds = -1);

    /// <summary>
    /// Releases the asynchronous lock associated with the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The unique identifier for the lock to release.</param>
    /// <returns>
    /// A <see cref="NullOperationResult"/> indicating whether the release was successful.
    /// </returns>
    public NullOperationResult TryExitAsyncLock(string key);
}
