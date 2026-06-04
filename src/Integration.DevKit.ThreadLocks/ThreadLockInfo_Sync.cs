/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

namespace Integration.DevKit.ThreadLocks;

/// <summary>
/// Represents the internal state and synchronization primitives for a synchronous named lock.
/// </summary>
/// <remarks>
/// This class is used by the <see cref="ThreadLockManager"/> to track the lifecycle 
/// and usage of a specific synchronous lock key.
/// </remarks>
internal sealed class ThreadLockInfo_Sync
{
    /// <summary>
    /// Gets the object used for <see langword="lock"/> statements or <see cref="Monitor"/> 
    /// calls to provide exclusive access.
    /// </summary>
    /// <value>
    /// A unique, read-only <see cref="object"/> instance.
    /// </value>
    public object LockObject { get; } = new object();

    /// <summary>
    /// Tracks the number of active references or threads waiting for this specific lock.
    /// </summary>
    public int RefCount = 0;

    /// <summary>
    /// Gets the timestamp of the last time this lock was successfully acquired or released.
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
