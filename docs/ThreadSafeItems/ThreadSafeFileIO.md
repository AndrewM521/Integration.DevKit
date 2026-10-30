# ThreadSafeFileIO

Part of [Integration.DevKit.ThreadSafeItems](README.md).

`ThreadSafeFileIO` wraps the file methods from `FileUtils` in [Integration.DevKit.Core](../Core/FileAndDirectoryUtils.md). Each call takes a [ThreadLocks](../ThreadLocks/README.md) lock keyed on the file path, so reads and writes to the same file never overlap. The method shapes match `FileUtils`, so switching an existing call is a one-line change.

## Getting an instance

After `Service_ThreadSafeItems.Initialize` has run (see [Getting started](README.md#getting-started)):

```csharp
var fileIO = Service_ThreadSafeItems.ThreadSafeFileIO;
```

It's also registered as a singleton, so you can inject `ThreadSafeFileIO` into your own services.

## How it works

Every method:

1. Takes the lock keyed by the **file path itself**. Async methods take an async lock, and sync methods take a sync lock.
2. Calls the matching `FileUtils` method.
3. Releases the lock in a `finally` block. A failure to release is logged, not thrown.

If the lock isn't acquired within `lockTimeoutMs` (default 5 seconds), the call returns a failed result wrapping a `TimeoutException` instead of blocking indefinitely.

## How to

### Write to a file

```csharp
// Overwrite
var write = await fileIO.WriteToFileAsync(path, "some content");

// Append a line
await fileIO.WriteToFileAsync(path, "another line", append: true);

// Write several lines at once
await fileIO.WriteToFileAsync(path, new[] { "line 1", "line 2" }, append: true);

if (!write.MethodSuccess)
{
    logger.LogError(write.Exception, "Write failed");
}
```

By default the path must have a file extension. Pass `allowNoFileExtension: true` to allow names like `LOG`. `encoding` defaults to UTF-8.

### Read a file

```csharp
var text = await fileIO.ReadFileTextAsync(path);
if (text.MethodSuccess)
{
    Console.WriteLine(text.Result);
}

var lines = await fileIO.ReadFileLinesAsync(path);
```

### Change the lock timeout

Every method takes `lockTimeoutMs`. Raise it for files that are held for a long time, or lower it to fail fast:

```csharp
var result = await fileIO.ReadFileTextAsync(path, lockTimeoutMs: 30_000);
```

### Don't mix sync and async calls on the same path

Sync and async locks in ThreadLocks don't exclude each other (see [ThreadLocks](../ThreadLocks/README.md#take-and-release-a-lock)). Calling `WriteToFile` (sync) and `WriteToFileAsync` (async) against the *same path* is not safe. Pick one style and use it consistently for any given file.

### Turn logging off at runtime

```csharp
fileIO.RuntimeSettings.EnableLogging = false;
```

See [Conditional logging](../Core/ConditionalLogging.md) for how this works. The startup value comes from the `Integration.DevKit:ThreadSafeItems` section (see [Configuration](README.md#configuration)).

## API reference

```csharp
public class ThreadSafeFileIO
{
    public ThreadSafeFileIO(ThreadLockManager threadLockManager, IOptions<ThreadSafeItemsSettings>? settings = null, ILoggerFactory? loggerFactory = null);   // throws ArgumentNullException if threadLockManager is null

    public ThreadSafeItemsSettings RuntimeSettings { get; set; }   // mutate in place to change EnableLogging at runtime

    // Async (async lock per path)
    Task<NullOperationResult> WriteToFileAsync(string path, string content, bool append = false, bool allowNoFileExtension = false, Encoding? encoding = null, int lockTimeoutMs = 5000);
    Task<NullOperationResult> WriteToFileAsync(string path, string[] content, bool append = false, bool allowNoFileExtension = false, Encoding? encoding = null, int lockTimeoutMs = 5000);
    Task<OperationResult<string[]>> ReadFileLinesAsync(string path, int lockTimeoutMs = 5000);
    Task<OperationResult<string>> ReadFileTextAsync(string path, int lockTimeoutMs = 5000);

    // Sync (sync lock per path)
    NullOperationResult WriteToFile(string path, string content, bool append = false, bool allowNoFileExtension = false, Encoding? encoding = null, int lockTimeoutMs = 5000);
    NullOperationResult WriteToFile(string path, string[] content, bool append = false, bool allowNoFileExtension = false, Encoding? encoding = null, int lockTimeoutMs = 5000);
    OperationResult<string[]> ReadFileLines(string path, int lockTimeoutMs = 5000);
    OperationResult<string> ReadFileText(string path, int lockTimeoutMs = 5000);
}
```

| Parameter | Meaning |
| --- | --- |
| `path` | Full path to the file. Also used as the lock key. |
| `content` | Text to write, or an array of lines. |
| `append` | `true` to append; `false` (default) to overwrite. |
| `allowNoFileExtension` | `true` to accept a file name without an extension. |
| `encoding` | Character encoding; UTF-8 when `null`. |
| `lockTimeoutMs` | How long to wait for the lock before failing. Default `5000`. |

## Error handling

No method throws for an expected failure. Each returns `NullOperationResult` or `OperationResult<T>`, so check `MethodSuccess` and read `Exception` on failure:

- **Lock timeout:** a failed result wrapping a `TimeoutException`.
- **I/O failure** (missing file, bad path, access denied): a failed result wrapping the exception from `FileUtils`.

Failures are also logged when `EnableLogging` is on.

## Best practices

- Use `ThreadSafeFileIO` instead of `FileUtils` whenever the same file might be accessed from more than one place at once.
- Use either the sync or the async methods for a given file, never both.
- Route every access to a shared file through `ThreadSafeFileIO`. A direct `FileUtils` or `File.*` call elsewhere bypasses the lock.
