# Integration.DevKit.ThreadLocks

Named, reference-counted sync and async locks you acquire by string key instead of by a shared lock object.

## What it is

`Integration.DevKit.ThreadLocks` provides `ThreadLockManager`: you acquire and release a lock by an arbitrary string key (a file path, a customer ID, a resource name) rather than a shared object reference. Both synchronous (`Monitor`-based) and asynchronous (`SemaphoreSlim`-based) locks are supported, with optional timeouts, and lock entries are cleaned up automatically once nothing holds them.

## Why it exists

C#'s `lock` and `SemaphoreSlim` need a shared object that every caller can reach. When the thing you want to protect is identified by a value — "this file path", "this customer" — you end up building a dictionary of lock objects, reference-counting them so the dictionary doesn't grow forever, and handling timeouts. This module does that once. It's also the foundation for [ThreadSafeItems](https://github.com/AndrewM521/AndrewM5.DevKit/blob/main/docs/ThreadSafeItems/README.md) and for the `FileSecretStore` in [CredentialMgmt](https://github.com/AndrewM521/AndrewM5.DevKit/blob/main/docs/CredentialMgmt/README.md), both of which lock on file paths.

## Requirements & installation

- .NET 8, 9 or 10
- [Integration.DevKit.Core](https://github.com/AndrewM521/AndrewM5.DevKit/blob/main/docs/Core/README.md)

```bash
dotnet add package Integration.DevKit.ThreadLocks
# or, from source
dotnet add reference src/Integration.DevKit.ThreadLocks/Integration.DevKit.ThreadLocks.csproj
```

NuGet: [Integration.DevKit.ThreadLocks](https://www.nuget.org/packages/Integration.DevKit.ThreadLocks)

## Getting started

```csharp
using Integration.DevKit.ThreadLocks;

services.AddThreadLocks(configuration);   // binds the Integration.DevKit:ThreadLocks section
// ... build the host ...
Service_ThreadLocks.Initialize(app.Services);

var lockManager = Service_ThreadLocks.ThreadLockManager;
```

## Configuration

`AddThreadLocks` binds the `Integration.DevKit:ThreadLocks` section. The only setting is `EnableLogging` (default `true`), checked fresh on every log call — flip it at runtime via `lockManager.RuntimeSettings.EnableLogging = false;` to silence this module's logging without detaching the `ILoggerFactory` you registered for the rest of the app.

```json
{
  "Integration.DevKit": {
    "ThreadLocks": {
      "EnableLogging": true
    }
  }
}
```

## How to

### Take and release a lock

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

## API reference

### `Service_ThreadLocks` (static)

```csharp
public static IServiceCollection AddThreadLocks(this IServiceCollection services, IConfiguration config);
public static void Initialize(IServiceProvider sp);
public static ThreadLockManager ThreadLockManager { get; }   // throws InvalidOperationException before Initialize
```

### `ThreadLockManager`

`ThreadLockManager` lives directly under `Integration.DevKit.ThreadLocks` — this module has no pluggable extension point, so there's no separate `Interfaces/`/`Abstractions/`/`Implementations/` split, just the one concrete class.

```csharp
public class ThreadLockManager
{
    NullOperationResult TryEnterSyncLock(string key, int timeoutMilliseconds = -1);
    NullOperationResult TryExitSyncLock(string key);

    Task<NullOperationResult> TryEnterAsyncLock(string key, int timeoutMilliseconds = -1);
    NullOperationResult TryExitAsyncLock(string key);   // exiting is not async even for the async lock
}
```

## Error handling

Lock acquisition reports failure through the SDK's result-object pattern (`NullOperationResult`) rather than throwing — check `MethodSuccess` and inspect `Exception` on failure. Lock timeouts specifically surface as a failed result wrapping a `TimeoutException`, so a caller can distinguish "didn't get the lock in time" from other failure modes by inspecting `Exception.GetType()` if needed.

## Best practices

- Never mix sync and async lock calls against the same key.
- Always pair `TryEnterX...` with `TryExitX...` in a `try`/`finally`.
- Use a bounded timeout in production code paths so a stuck holder can't block callers forever.
- For file access, use [ThreadSafeItems](https://github.com/AndrewM521/AndrewM5.DevKit/blob/main/docs/ThreadSafeItems/README.md) rather than locking around `FileUtils` yourself.

## Changelog

See [CHANGELOG.md](https://github.com/AndrewM521/AndrewM5.DevKit/blob/main/docs/ThreadLocks/CHANGELOG.md).
