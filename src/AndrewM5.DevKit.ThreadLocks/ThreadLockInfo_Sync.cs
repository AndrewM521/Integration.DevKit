namespace AndrewM5.DevKit.ThreadLocks;

/// <summary>
/// Represents internal state and synchronization primitives for a synchronous named lock.
/// </summary>
internal sealed class ThreadLockInfo_Sync
{
    /// <summary>
    /// Gets the object used for <see langword="lock"/> statements to provide exclusive access.
    /// </summary>
    public object LockObject { get; } = new object();

    /// <summary>
    /// Tracks the number of active references or threads waiting for this specific lock.
    /// </summary>
    /// <remarks>
    /// Used by the manager to determine when this lock metadata can be safely evicted from memory.
    /// </remarks>
    public int RefCount = 0;

    /// <summary>
    /// The timestamp of the last time this lock was accessed or updated.
    /// </summary>
    public DateTime LastAccessTime = DateTime.MinValue;

    /// <summary>
    /// Updates the <see cref="LastAccessTime"/> to the current UTC time.
    /// </summary>
    public void UpdateLastAccessTime()
    {
        LastAccessTime = DateTime.UtcNow;
    }
}
