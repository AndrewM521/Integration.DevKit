namespace AndrewM5.DevKit.ThreadLocks;

/// <summary>
/// Represents internal state and synchronization primitives for an asynchronous named lock.
/// </summary>
internal sealed class ThreadLockInfo_Async : IDisposable
{
    /// <summary>
    /// Gets the <see cref="SemaphoreSlim"/> used to provide exclusive access to the resource.
    /// </summary>
    public SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(1, 1);

    /// <summary>
    /// Tracks the number of active references or waiters for this specific lock.
    /// </summary>
    /// <remarks>
    /// This is typically used to determine when the lock object can be safely 
    /// removed from a central cache.
    /// </remarks>
    public int RefCount = 0;

    /// <summary>
    /// The timestamp of the last time this lock was accessed or updated.
    /// </summary>
    public DateTime LastAccessTime = DateTime.MinValue;

    /// <summary>
    /// Disposes the underlying <see cref="SemaphoreSlim"/>.
    /// </summary>
    public void Dispose()
    {
        Semaphore.Dispose();
    }

    /// <summary>
    /// Updates the <see cref="LastAccessTime"/> to the current UTC time.
    /// </summary>
    public void UpdateLastAccessTime()
    {
        LastAccessTime = DateTime.UtcNow;
    }
}
