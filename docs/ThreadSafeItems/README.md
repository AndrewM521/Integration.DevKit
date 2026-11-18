# Integration.DevKit.ThreadSafeItems

Thread-safe wrappers around shared resources, built on [ThreadLocks](../ThreadLocks/README.md) so concurrent callers don't race on the same item.

## What it is

`Integration.DevKit.ThreadSafeItems` is a set of classes that guard a shared resource with a ThreadLocks lock keyed on that resource, so concurrent callers are serialized instead of racing. It includes:

| Class | What it does |
| --- | --- |
| [`ThreadSafeFileIO`](ThreadSafeFileIO.md) | Wraps the `FileUtils` methods from [Integration.DevKit.Core](../Core/FileAndDirectoryUtils.md) with an async lock keyed on the file's full path, so reads and writes to the same file never overlap, however the path is spelled. |

All classes are reached through `Service_ThreadSafeItems` after it's initialized (see [Getting started](#getting-started)). Each class has its own page with how-to guides and its full API.

## Why it exists

Two tasks using the same resource at once can corrupt it or fail. For example, two tasks appending to the same log file can interleave writes or fail with "file in use" errors. Fixing that by hand means a lock per resource, a timeout, and a `try`/`finally` around every call. The classes in this module do that for you and keep the same method shapes as the code they wrap. For example, `ThreadSafeFileIO` matches `FileUtils`, so switching is a one-line change at the call site.

## Requirements & installation

- [Integration.DevKit.ThreadLocks](../ThreadLocks/README.md) (pulled in automatically; must be registered and initialized first — see below)

NuGet: [Integration.DevKit.ThreadSafeItems](https://www.nuget.org/packages/Integration.DevKit.ThreadSafeItems)

```bash
dotnet add package Integration.DevKit.ThreadSafeItems
```

## Getting started

```csharp
using Integration.DevKit.ThreadLocks;
using Integration.DevKit.ThreadSafeItems;

services.AddThreadSafeItems(configuration);    // also registers ThreadLocks; binds the Integration.DevKit:ThreadSafeItems section
// ... build the host ...
Service_ThreadLocks.Initialize(app.Services);       // only needed if you also use Service_ThreadLocks directly
Service_ThreadSafeItems.Initialize(app.Services);

var fileIO = Service_ThreadSafeItems.ThreadSafeFileIO;
await fileIO.WriteToFileAsync(path, "some content");
```

`AddThreadSafeItems` registers the ThreadLocks module itself, so a separate `AddThreadLocks()` call is optional. `Service_ThreadSafeItems.Initialize` still checks that a `ThreadLockManager` can be resolved, in case the services were registered by hand.

## Configuration

`AddThreadSafeItems` binds the `Integration.DevKit:ThreadSafeItems` section.

```json
{
  "Integration.DevKit": {
    "ThreadSafeItems": {
      "EnableLogging": true
    }
  }
}
```

| Property | Default | Purpose |
| --- | --- | --- |
| `EnableLogging` | `true` | Turns this module's logging on or off. Change it at runtime with `fileIO.RuntimeSettings.EnableLogging = false;`; see [Conditional logging](../Core/ConditionalLogging.md) for how this works. |

## How to

### Read and write files safely

Use [`ThreadSafeFileIO`](ThreadSafeFileIO.md). It covers writing, appending, reading, lock timeouts and why you shouldn't mix sync and async calls on the same path.

## API reference

### `Service_ThreadSafeItems` (static)

```csharp
public static IServiceCollection AddThreadSafeItems(this IServiceCollection services, IConfiguration config);
public static void Initialize(IServiceProvider sp);   // throws if no ThreadLockManager is registered
public static ThreadSafeFileIO ThreadSafeFileIO { get; }   // throws InvalidOperationException before Initialize
```

### Classes

| Class | API reference |
| --- | --- |
| `ThreadSafeFileIO` | [ThreadSafeFileIO.md](ThreadSafeFileIO.md#api-reference) |

## Error handling

Every method reports failure through the SDK's result-object pattern (`OperationResult<T>` / `NullOperationResult`) rather than throwing — check `MethodSuccess` and inspect `Exception` on failure. A lock timeout surfaces as a failed result wrapping a `TimeoutException`.

## Best practices

- See each class's page for its own best practices, e.g. [`ThreadSafeFileIO`](ThreadSafeFileIO.md#best-practices).
- Register and initialize `ThreadLocks` before `ThreadSafeItems` — the latter will throw at startup if the former isn't present.

## Changelog

See [CHANGELOG.md](CHANGELOG.md).
