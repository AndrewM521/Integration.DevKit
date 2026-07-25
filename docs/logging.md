# Custom Logging

`Integration.DevKit.CustomLogger` and `Integration.DevKit.CustomLogger.Flusher` implement a lightweight `Microsoft.Extensions.Logging`-compatible logger with three independent output sinks — an always-on debug sink, an optional console sink, and an optional buffered file sink — plus a background service that periodically flushes the file buffer to disk.

## Requirements

- .NET 8
- `Microsoft.Extensions.Logging.Abstractions`
- A built `IServiceProvider` (both modules follow the SDK-wide register → build → initialize pattern)

## Installation

```bash
dotnet add reference src/Integration.DevKit.CustomLogger/Integration.DevKit.CustomLogger.csproj
dotnet add reference src/Integration.DevKit.CustomLogger.Flusher/Integration.DevKit.CustomLogger.Flusher.csproj
```

The flusher project depends on the logger project; you can take the logger without the flusher if you don't need buffered file output, but not the reverse.

## Getting started

```csharp
using Integration.DevKit.CustomLogger;
using Integration.DevKit.CustomLogger.Flusher;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddCustomLogging(configuration);      // must come first
        services.AddCustomLogFlusher(configuration);   // depends on AddCustomLogging
    });

var app = builder.Build();

Service_CustomLogger.Initialize(app.Services);          // must come first
Service_CustomLogFlusher.Initialize(app.Services);      // depends on Service_CustomLogger's registrations

await app.RunAsync();   // the flusher's background loop only runs once the host is running

var logger = Service_CustomLogger.LoggerManager.GetLogger("MyApp");
logger.LogInformation("Application started");
```

**Ordering matters.** `Service_CustomLogFlusher.Initialize` explicitly checks that the logging module's services are already registered and throws `InvalidOperationException` if `AddCustomLogging` wasn't called first. Register both, build the provider, then initialize both, in that order.

## Configuration

| Module | Config section | Bound to |
| --- | --- | --- |
| `AddCustomLogging` | `Integration.DevKit:CustomLogger` | `LoggerManagerSettings` |
| `AddCustomLogFlusher` | `Integration.DevKit:CustomLoggerFlusher` | `LogFlushServiceSettings` |

```json
{
  "Integration.DevKit": {
    "CustomLogger": {
      "OutputLogLevel": "Debug",
      "FileOutputLogLevel": "Information"
    },
    "CustomLoggerFlusher": {
      "CreateLogFile": true,
      "AllowCreateFileInContainer": true,
      "MaxBufferCount": 40,
      "FlushIntervalSeconds": 10,
      "LogFilePath": "C:\\logs\\app.log"
    }
  }
}
```

### `LoggerManagerSettings`

| Property | Default | Purpose |
| --- | --- | --- |
| `OutputLogLevel` | `LogLevel.Debug` | Minimum level a message must meet to be processed at all (gates `ICustomLogger.IsEnabled`). |
| `FileOutputLogLevel` | `LogLevel.Information` | A **second, independent** minimum level a message must meet to be queued for file output. A message can pass `OutputLogLevel` and still be excluded from the file buffer if it's below this. |

### `LogFlushServiceSettings`

| Property | Default | Purpose |
| --- | --- | --- |
| `CreateLogFile` | `false` | Master switch for writing the buffer to disk at all. |
| `AllowCreateFileInContainer` | `false` | When running inside a container (detected via `DOTNET_RUNNING_IN_CONTAINER=true`), file writes are additionally gated by this flag. |
| `MaxBufferCount` | `50` | Flush is triggered once the in-memory queue reaches this many messages. |
| `FlushIntervalSeconds` | `30` | Flush is also triggered once this much time has elapsed since the last flush, regardless of buffer size. |
| `LogFilePath` | `""` | Destination file. Writes always **append** — there is no log rotation or size cap, so plan your own rotation/cleanup if `LogFilePath` is long-lived. |

> **Buffer-retention gotcha.** If `CreateLogFile` is `false`, the flusher never drains the in-memory queue — messages simply accumulate for as long as the process runs, which can grow unbounded under sustained logging. If you're running in a container and `AllowCreateFileInContainer` is `false` while `CreateLogFile` is `true`, the opposite happens: the queue **is** drained on each flush tick but the messages are silently discarded (never written anywhere). If you need guaranteed console/debug-only logging with a bounded process, prefer `CreateLogFile = true` with a real `LogFilePath`.

## Getting a logger

```csharp
var logger = Service_CustomLogger.LoggerManager.GetLogger("MyApp");
```

`ICustomLoggerManager.GetLogger(string categoryName)` (`CustomLoggerManager`) caches one `ICustomLogger` per category name (case-insensitive), so repeated calls with the same name return the same instance.

## `ICustomLogger`

```csharp
namespace Integration.DevKit.CustomLogger.Contracts;

public interface ICustomLogger : ILogger
{
    string CategoryName { get; }
    bool IsLoggerEnabled { get; }

    void EnableLogger();
    void DisableLogger();
    void EnableConsoleOutput();
    void DisableConsoleOutput();
}
```

`ICustomLogger` extends the standard `Microsoft.Extensions.Logging.ILogger`, so it works with `LogInformation`, `LogWarning`, `LogError`, etc. as usual, in addition to the members above.

```csharp
logger.EnableConsoleOutput();
logger.LogWarning("This will now also print to the console");
logger.DisableConsoleOutput();
```

### Where a log message actually goes

Every enabled message (one that passes `IsEnabled`, i.e. meets `OutputLogLevel`) is written to up to three destinations independently:

1. **`System.Diagnostics.Debug.WriteLine`** — always, unconditionally, with no way to turn it off. This is not configurable and isn't mentioned by `EnableConsoleOutput`/`DisableConsoleOutput` — treat it as a permanent debug-attach sink.
2. **The console** — only while `EnableConsoleOutput()` is active for that logger instance (off by default). Uses a shorter format with no PID/timestamp prefix.
3. **The in-memory file buffer** (`ILogFileRegistry`) — only if the message also meets the separate `FileOutputLogLevel` threshold. This buffer is drained periodically by `LogFlusher` (see below); nothing writes to disk until the flusher does.

`BeginScope<TState>` is implemented but is a no-op (returns a do-nothing `IDisposable`) — scoped logging (`using (logger.BeginScope(...))`) will not add scope data to messages.

## `LogFlusher`

```csharp
namespace Integration.DevKit.CustomLogger.Flusher;

public class LogFlusher : BackgroundService, ILogFlusher
{
    public LogFlushServiceSettings RuntimeSettings { get; }
    public void LogRuntimeSettings();
}
```

`LogFlusher` is a standard `BackgroundService` — it is registered as an `IHostedService` and only starts polling once the host itself is running (`app.RunAsync()`/`host.Run()`); calling `Service_CustomLogFlusher.Initialize` alone does not start the flush loop. On each tick (every 500ms) it checks whether the buffer has reached `MaxBufferCount` or `FlushIntervalSeconds` has elapsed, and if so writes the buffered messages to `LogFilePath` via `FileUtils.WriteToFile(..., append: true)`. It performs one final flush when the host shuts down. Any exception during a flush is caught, logged to `Debug.WriteLine` only, and swallowed — a flush failure never throws out of the background loop.

## API Reference

### `Service_CustomLogger` (static)

```csharp
public static IServiceCollection AddCustomLogging(this IServiceCollection services, IConfiguration config);
public static void Initialize(IServiceProvider sp);
public static ICustomLoggerManager LoggerManager { get; }   // throws InvalidOperationException before Initialize
public static ILogFileRegistry LogRegistry { get; }          // throws InvalidOperationException before Initialize
```

### `Service_CustomLogFlusher` (static)

```csharp
public static IServiceCollection AddCustomLogFlusher(this IServiceCollection services, IConfiguration config);
public static void Initialize(IServiceProvider sp);   // throws if AddCustomLogging wasn't called first
public static ILogFlusher LogFlushService { get; }     // throws InvalidOperationException before Initialize
```

### `ICustomLoggerManager`

```csharp
public LoggerManagerSettings RuntimeSettings { get; }
public ICustomLogger GetLogger(string categoryName);
public void LogRuntimeSettings();   // logs every RuntimeSettings property at Debug level, for diagnostics
```

## Error handling

Logging calls themselves do not throw under normal use. `Initialize` on either static class throws `InvalidOperationException` if the corresponding services weren't registered first (or, for the flusher, if the logging module wasn't registered before the flusher module) — this is a startup-time configuration error, not something to catch per log call.

## Best Practices

- Call `AddCustomLogging` before `AddCustomLogFlusher`, and `Service_CustomLogger.Initialize` before `Service_CustomLogFlusher.Initialize`.
- Set `FileOutputLogLevel` deliberately — it's easy to assume `OutputLogLevel` alone controls what reaches the log file.
- If you enable `CreateLogFile`, make sure `LogFilePath` points somewhere with rotation/retention handled externally, since the flusher only ever appends.
- Avoid leaving `CreateLogFile = false` in a long-running, high-log-volume process — the buffer has no upper bound beyond the in-memory queue itself.
- Use console output for local development only; leave it disabled in production to avoid unnecessary I/O.
