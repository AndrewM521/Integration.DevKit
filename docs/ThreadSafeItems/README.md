# Integration.DevKit.ThreadSafeItems

Thread-safe file I/O: Core's file helpers wrapped in a per-path lock so concurrent callers don't race on the same file.

## What it is

`Integration.DevKit.ThreadSafeItems` provides `ThreadSafeFileIO`, which wraps the `FileUtils` methods from [Integration.DevKit.Core](https://github.com/AndrewM521/AndrewM5.DevKit/blob/main/docs/Core/README.md#file-and-directory-utilities) with a [ThreadLocks](https://github.com/AndrewM521/AndrewM5.DevKit/blob/main/docs/ThreadLocks/README.md) async lock keyed on the file path. Concurrent callers writing to or reading from the same file are serialized instead of racing.

## Why it exists

Two tasks appending to the same log or output file at once can interleave writes or fail with "file in use" errors. Fixing that by hand means a lock per file path, a timeout, and a `try`/`finally` around every call. `ThreadSafeFileIO` does that for you with the same method shapes as `FileUtils`, so switching is a one-line change at the call site.

## Requirements & installation

- .NET 8, 9 or 10
- [Integration.DevKit.ThreadLocks](https://github.com/AndrewM521/AndrewM5.DevKit/blob/main/docs/ThreadLocks/README.md) (pulled in automatically; must be registered and initialized first — see below)

```bash
dotnet add package Integration.DevKit.ThreadSafeItems
# or, from source
dotnet add reference src/Integration.DevKit.ThreadSafeItems/Integration.DevKit.ThreadSafeItems.csproj
```

NuGet: [Integration.DevKit.ThreadSafeItems](https://www.nuget.org/packages/Integration.DevKit.ThreadSafeItems)

## Getting started

```csharp
using Integration.DevKit.ThreadLocks;
using Integration.DevKit.ThreadSafeItems;

services.AddThreadLocks(configuration);        // required first
services.AddThreadSafeItems(configuration);    // binds the Integration.DevKit:ThreadSafeItems section
// ... build the host ...
Service_ThreadLocks.Initialize(app.Services);       // required first
Service_ThreadSafeItems.Initialize(app.Services);

var fileIO = Service_ThreadSafeItems.ThreadSafeFileIOClass;   // note the exact property name
await fileIO.WriteToFileAsync(path, "some content");
```

`Service_ThreadSafeItems.Initialize` explicitly checks that `ThreadLockManager` is resolvable and throws `InvalidOperationException` telling you to call `AddThreadLocks()` first if it isn't — the ordering above isn't optional. The static accessor is named **`ThreadSafeFileIOClass`**, not `ThreadSafeFileIO` (the class itself is `ThreadSafeFileIO`; only the static property has the `Class` suffix) — easy to mistype from memory.

## Configuration

`AddThreadSafeItems` binds the `Integration.DevKit:ThreadSafeItems` section. The only setting is `EnableLogging` (default `true`), checked fresh on every log call — flip it at runtime via `fileIO.RuntimeSettings.EnableLogging = false;`.

```json
{
  "Integration.DevKit": {
    "ThreadSafeItems": {
      "EnableLogging": true
    }
  }
}
```

## How to

### Read and write files safely

Every method acquires an async lock keyed by the **file path itself** before delegating to the matching `FileUtils` method, then releases it in a `finally` block (any failure releasing the lock is logged but swallowed). If the lock isn't acquired within `lockTimeoutMs` (default 5 seconds), the call returns a failed result wrapping a `TimeoutException` rather than blocking indefinitely.

```csharp
var write = await fileIO.WriteToFileAsync(path, "line", append: true);
var read = await fileIO.ReadFileTextAsync(path);
if (read.MethodSuccess)
{
    Console.WriteLine(read.Result);
}
```

> **Don't mix sync and async calls on the same path.** Sync and async locks in ThreadLocks don't exclude each other (see [ThreadLocks](https://github.com/AndrewM521/AndrewM5.DevKit/blob/main/docs/ThreadLocks/README.md#take-and-release-a-lock)), so mixing `WriteToFile` (sync) and `WriteToFileAsync` (async) calls against the *same path* is not actually safe — pick one and use it consistently for any given file.

## API reference

### `Service_ThreadSafeItems` (static)

```csharp
public static IServiceCollection AddThreadSafeItems(this IServiceCollection services, IConfiguration config);
public static void Initialize(IServiceProvider sp);   // throws if AddThreadLocks wasn't called first
public static ThreadSafeFileIO ThreadSafeFileIOClass { get; }   // throws InvalidOperationException before Initialize
```

### `ThreadSafeFileIO`

```csharp
public ThreadSafeFileIO(ThreadLockManager threadLockManager, IOptions<ThreadSafeItemsSettings>? settings = null, ILoggerFactory? loggerFactory = null);

public ThreadSafeItemsSettings RuntimeSettings { get; set; }   // mutate in place to change EnableLogging at runtime

Task<NullOperationResult> WriteToFileAsync(string path, string content, bool append = false, bool allowNoFileExtension = false, Encoding? encoding = null, int lockTimeoutMs = 5000);
Task<NullOperationResult> WriteToFileAsync(string path, string[] content, ...);
Task<OperationResult<string[]>> ReadFileLinesAsync(string path, int lockTimeoutMs = 5000);
Task<OperationResult<string>> ReadFileTextAsync(string path, int lockTimeoutMs = 5000);

// WriteToFile / ReadFileLines / ReadFileText: synchronous equivalents, same parameters
```

## Error handling

Every method reports failure through the SDK's result-object pattern (`OperationResult<T>` / `NullOperationResult`) rather than throwing — check `MethodSuccess` and inspect `Exception` on failure. A lock timeout surfaces as a failed result wrapping a `TimeoutException`.

## Best practices

- Never mix sync and async calls against the same path.
- Prefer `ThreadSafeFileIO` over direct `FileUtils` calls whenever the same file might be accessed concurrently from more than one place.
- Register and initialize `ThreadLocks` before `ThreadSafeItems` — the latter will throw at startup if the former isn't present.

## Changelog

See [CHANGELOG.md](https://github.com/AndrewM521/AndrewM5.DevKit/blob/main/docs/ThreadSafeItems/CHANGELOG.md).
