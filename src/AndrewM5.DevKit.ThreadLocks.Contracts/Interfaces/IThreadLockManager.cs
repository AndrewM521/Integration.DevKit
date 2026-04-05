using AndrewM5.DevKit.Core.Results;

namespace AndrewM5.DevKit.ThreadLocks.Contracts.Interfaces;

/// <summary>
/// Defines a contract for managing named synchronization locks in both synchronous and asynchronous contexts.
/// </summary>
public interface IThreadLockManager
{
    /// <summary>
    /// Attempts to acquire a synchronous lock associated with the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The unique identifier for the lock.</param>
    /// <param name="timeoutMilliseconds">
    /// The number of milliseconds to wait for the lock. 
    /// Defaults to -1, which represents an infinite wait.
    /// </param>
    /// <returns>
    /// A <see cref="NullOperationResult"/> indicating whether the lock was successfully acquired.
    /// </returns>
    public NullOperationResult TryEnterSyncLock(string key, int timeoutMilliseconds = -1);

    /// <summary>
    /// Releases the synchronous lock associated with the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The unique identifier for the lock to release.</param>
    /// <returns>
    /// A <see cref="NullOperationResult"/> indicating the success or failure of the release operation.
    /// </returns>
    public NullOperationResult TryExitSyncLock(string key);

    /// <summary>
    /// Attempts to acquire an asynchronous lock associated with the specified <paramref name="key"/>.
    /// </summary>
    /// <param name="key">The unique identifier for the lock.</param>
    /// <param name="timeoutMilliseconds">
    /// The number of milliseconds to wait for the lock. 
    /// Defaults to -1, which represents an infinite wait.
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
    /// A <see cref="NullOperationResult"/> indicating the success or failure of the release operation.
    /// </returns>
    public NullOperationResult TryExitAsyncLock(string key);
}
