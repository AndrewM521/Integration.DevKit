# Changelog — Integration.DevKit.ProcessLauncher

This changelog covers only the `Integration.DevKit.ProcessLauncher` package. See the [root changelog](../CHANGELOG.md) for the full SDK release notes.

## [3.0.0.0] - 2026-11-16 (current)

### Removed
- **Breaking:** `ManagedProcessConfig.EnableProcessLogging` and `ManagedProcessConfig.AutoRestartOnFailure`. Neither ever had any effect.

### Added
- `ManagedProcessConfig.MaxOutputLines` (default `10_000`): the most stdout and stderr lines kept per process. Older lines are dropped; `0` or less keeps everything.
- `ProcessManager` takes an optional `IHostApplicationLifetime` and cancels every running process on `ApplicationStopping`, so child processes no longer outlive the app (a restart used to leave the old process running next to the new one). `ProcessManager` is also `IAsyncDisposable` and cancels its processes on dispose.

### Fixed
- A process that exited before `Start()` returned could stay registered as running forever, so `IsRunning` stayed true and the key could never be started again. Processes are now registered before they start, and the `Exited` handler only removes that exact process.
- Two concurrent `StartProcess` calls with the same key could both pass the "already running" check. Registration is now a single atomic `TryAdd`.
- Captured output grew without limit, and lines were appended from background threads while `GetOutput()`/`GetError()` could be reading the same `StringBuilder`, which isn't thread-safe. Output is now capped and every access is locked.
- `CancelProcess(key, forceKill: false)` (and `CancelAllProcesses`, including on app shutdown) always force killed the process within milliseconds: the background monitor saw the cancel and killed it while the graceful close was still waiting. The process now gets its 3 seconds to close before it is killed.
- `CancelProcess` removed the process from the running list before stopping it, so when the kill failed (e.g. access denied) the process kept running untracked: `IsRunning` returned `false` and the same key could start a second copy. It now cancels first and removes that exact process only on success.

### Changed
- `CancelAllProcesses` cancels processes in parallel, so the graceful 3-second wait per process doesn't add up past a host's shutdown timeout.
- `ProcessManager.WaitForExitAsync` now uses .NET's `Process.WaitForExitAsync` instead of checking `HasExited` every 250 ms, so it returns as soon as the process exits. With redirected output it also waits until all output has been read. It accepts a `null` process and returns at once.
- Errors passed up inside the module keep their original stack trace (see the Core changelog).
- Version bumped from `2.1.1.0` to `3.0.0.0`. Major version bump because of the removed options above.

## [2.1.1.0] - 2026-10-30

### Added
- The NuGet package now includes this module's README (`docs/ProcessLauncher/README.md`), shown on the package's nuget.org page.

### Changed
- Version bumped from `2.1.0.0` to `2.1.1.0`.

## [2.1.0.0] - 2026-10-28

### Changed
- Now multi-targets `net8.0`, `net9.0` and `net10.0` (previously `net8.0` only). Building all three targets requires the .NET 10 SDK.
- `Microsoft.Extensions.*` packages bumped to `10.0.14`.

### Notes
- Behavior from the `Microsoft.Extensions.*` 10.x packages applies on every target: configuration binding keeps JSON `null` as `null` instead of `""`.

## [2.0.0.0] - 2026-09-18

### Changed
- Version bumped from `1.2.0.0-preview` to `2.0.0.0`; strict `Major.Minor.Build.0` numbering from here on.
- Interfaces folded into the main package; the separate `ProcessLauncher.Contracts` package was removed.
- Unit tests added; files and folders reorganized.
- NuGet package metadata added.

### Fixed
- Process timeout issue.

## [1.0.0.x] - 2026-06-05 to 2026-06-25

### Added
- First versioned release (`1.0.0.1`).

### Changed
- `AndrewM5.DevKit` namespace renamed to `Integration.DevKit`.
- Host and service-collection setup combined into a single `Service_ProcessLauncher` class.

## Pre-1.0 (unversioned) - 2026-01-28 to 2026-06-04

### Changed
- `ProcessLauncher.Abstractions` split out of the main module, then replaced by a `.Contracts` project (2026-03-27).
