# Thread Safe Items Module: Quick Start
The ThreadSafeItems module provides high-level utilities for common operations that require synchronization by using the ThreadLockManager to lock access 
1. ThreadSafeFileIO - Ensures that file read/write operations are protected from race conditions based on the file path.

## 1. Access and Configuration 
This module acts as a consumer of the ThreadLocks and CustomLogger modules. It automates the "lock-try-finally-exit" pattern so you don't 
have to write boilerplate synchronization code for every file operation.

Namespace: Integration.DevKit.ThreadSafeItems

Dependencies: Requires IThreadLockManager to be registered in the service container.

ThreadSafeFileIO: Uses the absolute file path as the unique key for locks. This ensures that two different threads trying to write to the same file 
will wait for each other, while threads writing to different files can proceed in parallel.

## 2. Setup
The ThreadSafeItems module follows the same host-based initialization pattern as the rest of the DevKit.
This involves registering the service and then initializing the static host provider.

Registration and Initalization
1. Add the thread locking services to your IServiceCollection:
```
.ConfigureServices((context, services) =>
{
    // ... other services
    services.AddThreadLocks(); // Required dependency
    services.AddThreadSafeItems();
})
```

2. After building the host, Initialize the ThreadSafeItemsHost to enable access throughout your application:

```
var host = builder.Build();

ThreadLocksHost.Initialize(host.Services); // Required dependency
ThreadSafeItemsHost.Initialize(host.Services);
```

## 3. Examples
Once initialized, you can perform file I/O without manually managing Monitor or SemaphoreSlim objects.

Asynchronous File Writing
Use this for non-blocking UI or high-throughput background tasks.

```
string filePath = "C:\\Data\\logs.txt";
string content = "New log entry";

// The lock is automatically acquired using the filePath as the key
var result = await ThreadSafeItemsHost.FileIO.WriteToFileAsync(filePath, content, append: true);
if (result.MethodSuccess)
{
    // File was written successfully and lock was released
}
```

Synchronous File Reading
Use this for standard blocking operations where you need to ensure the file isn't being modified during the read.

```
string configPath = "C:\\Configs\\settings.json";

// Attempts to get a sync lock for 5 seconds (default) before reading
var readResult = ThreadSafeItemsHost.FileIO.ReadFileText(configPath);

if (readResult.MethodSuccess)
{
    string data = readResult.Result;
    Console.WriteLine(data);
}
else if (readResult.Exception is TimeoutException)
{
    // Handle case where the file was locked by another thread for too long
}
```

## [NOTE]
Internal Logging: If an operation fails or a lock cannot be released, the module automatically logs the error through the ICustomLogger 
if it was registered during setup, categorized under the "ThreadSafeFileIO" source.