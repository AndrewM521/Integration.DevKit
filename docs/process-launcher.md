# Process Launcher

`Integration.DevKit.ProcessLauncher` starts and supervises external processes (`cmd.exe`, a CLI tool, another executable) with output capture, cancellation, and an optional timeout, using the SDK's result-object pattern instead of raw `System.Diagnostics.Process` exception handling.

> **Security warning — only launch applications you trust.** `ManagedProcessConfig.Command`/`Arguments` are executed directly via `System.Diagnostics.Process` with no sandboxing, code signing check, or validation of any kind. This module provides supervision (timeout, cancellation, output capture) — it does **not** provide isolation or safety. Treat it the same as any direct call to `Process.Start`:
>
> - Never pass `Command` or `Arguments` built from untrusted input (user-supplied text, data from an external API, an uploaded file's contents/path, etc.) without strict validation — this is a command-injection risk, not just a reliability concern.
> - A launched process runs with the same OS-level privileges and user account as your application, and has full access to anything that account can reach (the file system, network, other local processes). It is not contained in any way by this SDK.
> - Only launch binaries/scripts from a trusted, ideally pinned/verified source (e.g. bundled with your app, or fetched from a location you control with integrity checks) — not something downloaded or referenced dynamically at runtime from an untrusted source.
> - `ShowWindow = false` captures stdout/stderr into memory (see below) but still does not limit what the child process can do; it only affects window visibility and stream redirection.

## Requirements

- .NET 8

## Installation

```bash
dotnet add reference src/Integration.DevKit.ProcessLauncher/Integration.DevKit.ProcessLauncher.csproj
```

Or from NuGet: [Integration.DevKit.ProcessLauncher](https://www.nuget.org/packages/Integration.DevKit.ProcessLauncher)

## Getting started

```csharp
using Integration.DevKit.ProcessLauncher;

services.AddProcessLauncher();
// ... build the host ...
Service_ProcessLauncher.Initialize(app.Services);

var processManager = Service_ProcessLauncher.ProcessManager;

var config = new ManagedProcessConfig
{
    ProcessKey = "PingTest",
    Command = "cmd.exe",
    Arguments = "/c ping 127.0.0.1 -n 4",
    ShowWindow = true,
    TimeoutSeconds = 10,   // 0 or negative (including the -1 default) means no timeout
    WorkingDirectory = Environment.CurrentDirectory
};

var started = processManager.StartProcess(config);
if (started.MethodSuccess)
{
    await processManager.WaitForExitAsync(started.Result.Process!);
}
```

`AddProcessLauncher()` takes no configuration section — there's nothing to bind from `appsettings.json`; every process is configured per-call via `ManagedProcessConfig`. There's also no ordering dependency on any other DevKit module: logging is used if `ICustomLoggerManager` happens to be registered, but it's optional.

## `ManagedProcessConfig`

```csharp
public class ManagedProcessConfig : IManagedProcessConfig
{
    public string ProcessKey { get; init; } = Guid.NewGuid().ToString();
    public string Command { get; init; } = string.Empty;
    public string Arguments { get; init; } = string.Empty;
    public bool ShowWindow { get; init; } = false;
    public string? WorkingDirectory { get; init; } = Environment.CurrentDirectory;
    public int TimeoutSeconds { get; set; } = -1;
    public bool EnableProcessLogging { get; set; } = true;
    public bool AutoRestartOnFailure { get; set; } = false;
}
```

| Property | Notes |
| --- | --- |
| `ProcessKey` | Must be unique among currently-running processes tracked by the same `ProcessManager` — starting a second process with a key already in use fails. |
| `ShowWindow` | Also controls whether stdout/stderr are captured — see the warning below. |
| `WorkingDirectory` | Defaults to the current directory **at the moment this config instance is constructed**, not re-evaluated later. |
| `TimeoutSeconds` | `0` or a negative value (including the `-1` default) means no timeout; a positive value is the wall-clock limit in seconds before the process is force-killed. |
| `EnableProcessLogging` | **Currently has no effect** — see below. |
| `AutoRestartOnFailure` | **Currently has no effect** — there is no auto-restart logic implemented anywhere in this module. Don't rely on it. |

> **stdout/stderr capture is tied to `ShowWindow`, not `EnableProcessLogging`.** Output is only captured when `ShowWindow == false` (internally this maps to `RedirectStandardOutput`/`RedirectStandardError` on the underlying `ProcessStartInfo`). If `ShowWindow == true`, `GetOutput()`/`GetError()` will always return empty strings, regardless of `EnableProcessLogging`. If you need to capture output, set `ShowWindow = false`.

## Starting and controlling a process

```csharp
public interface IProcessManager
{
    OperationResult<IManagedProcess> StartProcess(IManagedProcessConfig config);
    NullOperationResult CancelProcess(string processKey, bool forceKill = false);
    NullOperationResult CancelAllProcesses(bool forceKill = false);
    OperationResult<bool> IsRunning(string processKey);
    Task WaitForExitAsync(Process process, CancellationToken token = default);
}
```

`StartProcess` fails (as a failed `OperationResult`, not a thrown exception) for an empty `Command` or a `ProcessKey` that's already running. `WaitForExitAsync` is a simple 250ms poll loop against `Process.HasExited` — it isn't event-driven, so don't expect sub-250ms exit detection latency.

> **A process that exits on its own removes itself from tracking immediately.** `ProcessManager` listens for `Process.Exited` and un-tracks the process (by `ProcessKey`) as soon as it fires — which happens before any external code gets a chance to call `CancelProcess` on it. If you call `CancelProcess("SomeKey")` after the process has already exited naturally, it will fail because the key is no longer tracked; check `IsRunning` first if that distinction matters to your caller.

```csharp
public interface IManagedProcess : IAsyncDisposable
{
    string ProcessKey { get; }
    Process? Process { get; }
    Task? MonitorTask { get; }
    DateTime StartTime { get; }

    NullOperationResult Cancel(bool forceKill = false);
    OperationResult<string> GetOutput();
    OperationResult<string> GetError();
}
```

`Cancel(forceKill: false)` (the default) attempts a graceful shutdown first — `CloseMainWindow()`, then waits up to 3 seconds — before force-killing the process tree if it's still running. `Cancel(forceKill: true)` kills immediately. `IManagedProcess` instances are only obtainable through `IProcessManager.StartProcess` — there is no public constructor.

```csharp
var cancelResult = processManager.CancelProcess("PingTest", forceKill: false);
if (!cancelResult.MethodSuccess)
{
    logger.LogWarning(cancelResult.Exception, "Could not cancel process (it may have already exited)");
}
```

## API Reference

### `Service_ProcessLauncher` (static)

```csharp
public static IServiceCollection AddProcessLauncher(this IServiceCollection services);
public static void Initialize(IServiceProvider sp);
public static IProcessManager ProcessManager { get; }   // throws InvalidOperationException before Initialize
```

### `ProcessManager` (constructible directly, unlike most other DevKit implementation classes)

```csharp
public ProcessManager(ICustomLoggerManager? loggerManager = null);
```

## Error Handling

Every public method on `IProcessManager`/`IManagedProcess` returns an `OperationResult<T>`/`NullOperationResult` for expected failure modes (missing process, duplicate key, empty command) rather than throwing.

## Best Practices

- Set an explicit `TimeoutSeconds` for anything that shouldn't be allowed to run forever — the `-1` default means no timeout.
- Set `ShowWindow = false` whenever you need to read `GetOutput()`/`GetError()`.
- Call `IsRunning(processKey)` before `CancelProcess(processKey)` if the process might have already exited on its own.
- Don't configure `AutoRestartOnFailure` expecting restart behavior — it's not implemented yet.
- Dispose of (`await using`) or explicitly cancel processes you start — `DisposeAsync()` cancels the internal monitor and cleans up the underlying `Process` object.
