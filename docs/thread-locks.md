# Thread Locks and Thread-Safe File I/O

This guide covers two related modules:

- **`Integration.DevKit.ThreadLocks`** — named, reference-counted mutual-exclusion primitives (synchronous and asynchronous).
- **`Integration.DevKit.ThreadSafeItems`** — file I/O helpers built on top of `ThreadLocks` so concurrent callers don't race on the same file.

## Requirements

- .NET 8

## Installation

```bash
dotnet add reference src/Integration.DevKit.ThreadLocks/Integration.DevKit.ThreadLocks.csproj
dotnet add reference src/Integration.DevKit.ThreadSafeItems/Integration.DevKit.ThreadSafeItems.csproj
```

Or from NuGet: [Integration.DevKit.ThreadLocks](https://www.nuget.org/packages/Integration.DevKit.ThreadLocks) &middot; [Integration.DevKit.ThreadSafeItems](https://www.nuget.org/packages/Integration.DevKit.ThreadSafeItems)

`ThreadSafeItems` depends on `ThreadLocks` — register and initialize `ThreadLocks` first (see below).

---

## Thread Locks

`Integration.DevKit.ThreadLocks` provides named, reference-counted locks — you acquire and release a lock by an arbitrary string key rather than a shared object reference, which makes it easy to lock around "this file path" or "this customer ID" without threading a shared lock object through your code.

### Getting started with Thread Locks

```csharp
using Integration.DevKit.ThreadLocks;

services.AddThreadLocks();   // no configuration section — there is no options class for this module
// ... build the host ...
Service_ThreadLocks.Initialize(app.Services);

var lockManager = Service_ThreadLocks.ThreadLockManager;
```

### `IThreadLockManager`

```csharp
public interface IThreadLockManager
{
    NullOperationResult TryEnterSyncLock(string key, int timeoutMilliseconds = -1);
    NullOperationResult TryExitSyncLock(string key);

    Task<NullOperationResult> TryEnterAsyncLock(string key, int timeoutMilliseconds = -1);
    NullOperationResult TryExitAsyncLock(string key);   // exiting is not async even for the async lock
}
```

- `timeoutMilliseconds = -1` (default) waits indefinitely; `0` tests and returns immediately; any other value is a bounded wait. A failure to acquire within the timeout is returned as a failed result wrapping a `TimeoutException` — it does not throw directly.
- Keys are trimmed and compared case-insensitively.
- There is no disposable lock handle (no `using var _ = lockManager.Enter(...)`) — you must pair enter/exit calls yourself, typically in a `try`/`finally`:

```csharp
var entered = lockManager.TryEnterSyncLock("customer-42", timeoutMilliseconds: 5000);
if (!entered.MethodSuccess)
{
    // timed out or another error — do not proceed as if the lock is held
    return;
}

try
{
    // critical section
}
finally
{
    lockManager.TryExitSyncLock("customer-42");
}
```

The async variant follows the same pattern with `await TryEnterAsyncLock(...)` / `TryExitAsyncLock(...)`.

> **Sync and async locks with the same key do not exclude each other.** Sync locks (`Monitor`-based, thread-affine) and async locks (`SemaphoreSlim`-based) are tracked in two separate internal dictionaries. Calling `TryEnterSyncLock("x")` from one place and `TryEnterAsyncLock("x")` from another will both succeed simultaneously — they are entirely independent locks that happen to share a key string. Pick one lock flavor per logical resource and use it consistently everywhere that resource is touched.

`ThreadLockManager` does not implement `IDisposable`; individual lock entries are cleaned up automatically once their reference count returns to zero, but there's no way to force-release everything at once (e.g. on shutdown).

---

## Thread-Safe File I/O

`ThreadSafeFileIO` wraps the `FileUtils` methods from [Integration.DevKit.Core](core.md#file-and-directory-utilities) with a `ThreadLocks` async lock keyed on the file path, so concurrent callers writing to or reading from the same file are serialized instead of racing.

### Getting started with Thread-Safe File I/O

```csharp
using Integration.DevKit.ThreadSafeItems;

services.AddThreadLocks();        // required first
services.AddThreadSafeItems();
// ... build the host ...
Service_ThreadLocks.Initialize(app.Services);       // required first
Service_ThreadSafeItems.Initialize(app.Services);

var fileIO = Service_ThreadSafeItems.ThreadSafeFileIOClass;   // note the exact property name
await fileIO.WriteToFileAsync(path, "some content");
```

`Service_ThreadSafeItems.Initialize` explicitly checks that `IThreadLockManager` is resolvable and throws `InvalidOperationException` telling you to call `AddThreadLocks()` first if it isn't — the ordering above isn't optional. The static accessor is named **`ThreadSafeFileIOClass`**, not `ThreadSafeFileIO` (the class itself is `ThreadSafeFileIO`; only the static property has the `Class` suffix) — easy to mistype from memory.

### `ThreadSafeFileIO`

```csharp
public ThreadSafeFileIO(IThreadLockManager threadLockManager, ICustomLoggerManager? loggerManager = null);

Task<NullOperationResult> WriteToFileAsync(string path, string content, bool append = false, bool allowNoFileExtension = false, Encoding? encoding = null, int lockTimeoutMs = 5000);
Task<NullOperationResult> WriteToFileAsync(string path, string[] content, ...);
Task<OperationResult<string[]>> ReadFileLinesAsync(string path, int lockTimeoutMs = 5000);
Task<OperationResult<string>> ReadFileTextAsync(string path, int lockTimeoutMs = 5000);

// WriteToFile / ReadFileLines / ReadFileText: synchronous equivalents, same parameters
```

Every method acquires an async lock keyed by the **file path itself** before delegating to the matching `FileUtils` method, then releases it in a `finally` block (any failure releasing the lock is logged but swallowed). Because sync and async locks don't exclude each other (see above), mixing `WriteToFile` (sync) and `WriteToFileAsync` (async) calls against the *same path* is not actually safe — pick one and use it consistently for any given file. If the lock isn't acquired within `lockTimeoutMs` (default 5 seconds), the call returns a failed result wrapping a `TimeoutException` rather than blocking indefinitely.

## API Reference

### `Service_ThreadLocks` (static)

```csharp
public static IServiceCollection AddThreadLocks(this IServiceCollection services);
public static void Initialize(IServiceProvider sp);
public static IThreadLockManager ThreadLockManager { get; }   // throws InvalidOperationException before Initialize
```

### `Service_ThreadSafeItems` (static)

```csharp
public static IServiceCollection AddThreadSafeItems(this IServiceCollection services);
public static void Initialize(IServiceProvider sp);   // throws if AddThreadLocks wasn't called first
public static ThreadSafeFileIO ThreadSafeFileIOClass { get; }   // throws InvalidOperationException before Initialize
```

## Error Handling

Lock acquisition and thread-safe file I/O both report failure through the SDK's result-object pattern (`OperationResult<T>` / `NullOperationResult`) rather than throwing — check `MethodSuccess` and inspect `Exception` on failure. Lock timeouts specifically surface as a failed result wrapping a `TimeoutException`, so a caller can distinguish "didn't get the lock in time" from other failure modes by inspecting `Exception.GetType()` if needed.

## Best Practices

- Never mix sync and async lock calls (`ThreadLockManager` or `ThreadSafeFileIO`) against the same key/path.
- Always pair `TryEnterX...` with `TryExitX...` in a `try`/`finally` when using `ThreadLockManager` directly.
- Prefer `ThreadSafeFileIO` over direct `FileUtils` calls whenever the same file might be accessed concurrently from more than one place.
- Register and initialize `ThreadLocks` before `ThreadSafeItems` — the latter will throw at startup if the former isn't present.
