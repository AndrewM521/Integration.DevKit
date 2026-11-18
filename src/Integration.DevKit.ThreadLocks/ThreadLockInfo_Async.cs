/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

namespace Integration.DevKit.ThreadLocks;

/// <summary>
/// Represents the internal state and synchronization primitives for an asynchronous named lock.
/// </summary>
/// <remarks>
/// This class is used by the <see cref="ThreadLockManager"/> to track the lifecycle 
/// and usage of a specific asynchronous lock key.
/// </remarks>
internal sealed class ThreadLockInfo_Async : IDisposable
{
    /// <summary>
    /// Gets the <see cref="SemaphoreSlim"/> used to provide exclusive access to the resource.
    /// </summary>
    /// <value>
    /// A <see cref="SemaphoreSlim"/> initialized with an initial and maximum count of 1 to act as a mutex.
    /// </value>
    public SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(1, 1);

    /// <summary>
    /// Tracks the number of active references or waiters for this specific lock.
    /// </summary>
    public int RefCount = 0;

    /// <summary>
    /// Disposes the underlying <see cref="SemaphoreSlim"/>.
    /// </summary>
    /// <remarks>
    /// This should only be called when <see cref="RefCount"/> is zero and the object 
    /// is being removed from the manager's cache.
    /// </remarks>
    public void Dispose()
    {
        Semaphore.Dispose();
    }
}
