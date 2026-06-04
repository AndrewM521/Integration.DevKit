# Thread Locks Module: Quick Start
The ThreadLockManager provides a centralized, key-based locking system supporting both synchronous and asynchronous operations. 
It manages resources efficiently using reference counting, automatically cleaning up internal lock objects when they are no longer in use.

## 1. Access
The ThreadLocks module utilizes a static "Host" wrapper to provide global access to the locking service after initialization.

Namespace: Integration.DevKit.ThreadLocks

Key Normalization: To prevent deadlocks caused by casing differences, all keys are automatically trimmed and normalized to lowercase (Invariant Culture).
Thread Safety: The manager uses ConcurrentDictionary and Interlocked operations to ensure the management of the locks themselves is thread-safe.

## 2. Setup
The ThreadLocks module follows the same host-based initialization pattern as the rest of the DevKit.
This involves registering the service and then initializing the static host provider.

Registration and Initalization
1. Add the thread locking services to your IServiceCollection:
```
.ConfigureServices((context, services) =>
{
    // ... other services
    services.AddThreadLocks();
})
```

2. After building the host, Initialize the ThreadLocksHost to enable access throughout your application:

```
var host = builder.Build();

ThreadLocksHost.Initialize(host.Services);
```

## 3. Examples
Once initialized, you can access the locking mechanisms via the ThreadLocksHost.

Synchronous Locking (Monitor-based)
Ideal for standard blocking operations. The manager uses Monitor internally to handle thread synchronization.

```
string lockKey = "GlobalUpdateKey";

// Try to enter a sync lock with a 5-second timeout
var result = ThreadLocksHost.ThreadLockManager.TryEnterSyncLock(lockKey, 5000);
if (result.MethodSuccess)
{
    try
    {
        // Execute thread-sensitive logic here
    }
    finally
    {
        ThreadLocksHost.ThreadLockManager.TryExitSyncLock(lockKey);
    }
}
```

Asynchronous Locking (SemaphoreSlim-based)
Used in async/await workflows to avoid blocking the calling thread while waiting for access.

```
string lockKey = "GlobalUpdateKey";

// Non-blocking wait for the lock
var result = await ThreadLocksHost.ThreadLockManager.TryEnterAsyncLock(lockKey, 5000);
if (result.MethodSuccess)
{
    try
    {
        // Execute thread-sensitive logic here    
    }
    finally
    {
        // Releases the semaphore and handles automatic cleanup
        ThreadLocksHost.ThreadLockManager.TryExitAsyncLock(lockKey);
    }
}
```

## [NOTE]
Automatic Cleanup: When the RefCount for a specific key reaches zero during an Exit call, the ThreadLockManager automatically 
removes the lock object from memory to prevent leaks in applications with high key cardinality.