# Changelog — Integration.DevKit.TaskMgmt

This changelog covers only the `Integration.DevKit.TaskMgmt` package. See the [root changelog](../CHANGELOG.md) for the full SDK release notes.

## [3.0.0.0] - 2026-11-16 (current)

### Removed
- **Breaking:** `TaskManager.LogRuntimeSettings`.
- **Breaking:** `TaskManager.AwaitAllTasksToFinish`. It only called `Task.WhenAll` on the list you passed in, and never looked at the manager's own tasks. Call `Task.WhenAll(tasks)` instead.

### Fixed
- Iteration history (`ManagedTaskSnapshot.IterationHistory`, a `SortedDictionary`) was updated by parallel iterations without a lock, which could corrupt it or fault iterations. Updates and `GetSnapshotInfo(showIterations: true)` now lock it.
- A `DoTaskWork` override that throws before returning a `Task` (a non-`async` method) was silently swallowed: nothing was logged, retries never ran, and the iteration stayed `Running`. It is now treated like any other faulted iteration.
- With `StopIteratingOnException` (the default), the faulted iteration's final state and exception were never written to the registry, so it stayed `Running` in the history. It is now recorded as `Faulted`.
- If the final registry update of a task failed, the task's global concurrency slot was never released, so with `MaxConcurrentTasks` set, new tasks could stop starting. The failure is now logged and the slot always released.
- `TimeStrategy_Hourly` and `TimeStrategy_Interval` computed the next run in local wall-clock time, so a daylight-saving change skipped or stretched a run (e.g. a 15-minute interval had a 75-minute gap when clocks went back). They now add the interval in UTC. `TimeStrategy_Daily` still schedules by local time of day.
- `MaxConcurrentTasks = 0` or `MaxConcurrentParallelTasks = 0` made a semaphore with no slots, so tasks or iterations waited forever. Zero now means no limit, the same as negative values.
- `TaskManager.Initialize()` disposed the old concurrency limiter while tasks were waiting on it (faulting them), and running tasks then released the new one (raising the effective limit). Each task now releases the limiter it acquired, and the old one is not disposed.
- Setup error messages named `AddTaskManager()`, which doesn't exist; they now name `AddTaskMgmt()`.
- Going over `MaxTaskRegistryCount` removed every snapshot in the registry, not just the oldest one: the trim loop never re-read the count.
- Starting a task whose key was already running removed the *running* task from the manager, so `CancelTask`, `CancelAllTasks` and the shutdown hook could no longer stop it, and a third start ran a second copy. The rejected start now leaves the running task alone.
- A time-based task whose start time was still ahead skipped its first run: with `CustomStartTime = 07:00` and the app started at 06:00, a daily task first ran at 07:00 the next day. The start time is now the first run.
- A task that kept failing kept every exception for its whole life (a task running every minute against an API down for a week held about 10,000). The task now keeps at most `MaxTaskIterationRegistryCount` exceptions (at least 1). Each iteration also starts its cancel watchdog once instead of once per retry.
- Every iteration a task ran was kept in memory for the task's whole life, even in sequential mode, which already waits for each one. A task running every second held about 2.6 million finished iterations after 30 days. Only iterations still running (or faulted ones, so they are still reported) are kept now.
- Starting a task key again after it finished reused the old run's snapshot: `Settings` showed the first run's settings, and the new run's iterations updated the old run's records, keeping their old start times. Each run now starts a fresh snapshot.
- The "running longer than 1 second after being canceled" warning was logged on every cancel, even for work that stopped at once: the 1-second grace delay never ran. It now only fires for work that is still running a second after the cancel.
- The task registry never logged (registry trimming, eviction warnings): it was created without the logger factory. It now logs, and follows `TaskManagerSettings.EnableLogging`.
- `TimeStrategy_Interval` with `FastForwardToPresent` stepped through every missed slot one by one, so a small interval with an old start date (e.g. 1 second, a year ago) spent tens of seconds of CPU before the first run. It now jumps to the latest missed slot directly.
- A task with `IterationExecutionMode = Parallel` (or `AllowParallelIterationExecution`) and no iteration limit never returned from `StartTask`: the iteration loop started each iteration before waiting for a concurrency slot, so nothing in it ever waited. It spun on the caller's thread and queued iterations without end (about 13 MB of memory a second). The loop now waits for a free slot before starting the next iteration.

### Changed
- **Behavior change:** `TimeStrategy_Interval` now throws `ArgumentOutOfRangeException` for a zero or negative interval. With `FastForwardToPresent` on, such an interval spun the catch-up loop forever on one CPU core.
- **Behavior change:** `TimeStrategySettings.SkipFirstIterationWait` and `TimeStrategySettings.FastForwardToPresent` now default to `false` (were `true`). With the new defaults the first iteration waits for its scheduled time instead of running immediately, and runs missed while the app was down or busy are executed back-to-back instead of skipped. Set either to `true` to keep the old behavior.
- **Breaking:** `ManagedTaskSnapshot.IterationHistory` is no longer public. Use `GetIterationHistory()`, which returns a copy taken under the history's lock, so it can be enumerated while parallel iterations are still updating it (enumerating the live dictionary could throw "Collection was modified").
- Retries now wait before the next attempt: 1s, 2s, 4s and so on, up to 60s. Before, a failed attempt was retried after 50 ms, so `MaxRetryCount = 0` (unlimited) hit a broken dependency about 20 times a second.
- **Behavior change:** `StartTask` now fails with `InvalidOperationException` when the `Time_IterationStrategy` instance it is given is already used by another running task. A time strategy remembers its last run, so two tasks sharing one instance each skipped every other slot. Give each task its own strategy instance.
- Removed a branch in `Time_IterationStrategy.WaitForReadyAsync` that could never run (the wait is always at least 1 second, clipped to a time that is already above zero). Had it run, it would have stored "now" instead of the scheduled time as the last run. No behavior change.
- Errors passed up inside the module keep their original stack trace (see the Core changelog).
- **Behavior change:** `StartTask` fails with `ArgumentException` for a task that runs iterations in parallel with no concurrency limit (`MaxConcurrentParallelTasks <= 0`), no iteration limit and no iteration strategy. Nothing would ever make it wait, so it would start `DoTaskWork` without end.
- Version bumped from `2.1.1.0` to `3.0.0.0`. Major version bump because of the removed API and behavior changes above.

## [2.1.1.0] - 2026-10-30

### Added
- The NuGet package now includes this module's README (`docs/TaskMgmt/README.md`), shown on the package's nuget.org page.

### Changed
- Version bumped from `2.1.0.0` to `2.1.1.0`.

## [2.1.0.0] - 2026-10-28

### Changed
- Now multi-targets `net8.0`, `net9.0` and `net10.0` (previously `net8.0` only). Building all three targets requires the .NET 10 SDK.
- `Microsoft.Extensions.*` packages bumped to `10.0.14`.

### Notes
- Behavior from the `Microsoft.Extensions.*` 10.x packages applies on every target: configuration binding keeps JSON `null` as `null` instead of `""`.

## [2.0.0.0] - 2026-09-18

### Added
- `TaskManager.Initialize()` to re-apply runtime settings after changing them.
- NuGet package metadata added.

### Changed
- Version bumped from `1.2.0.0-preview` to `2.0.0.0`; strict `Major.Minor.Build.0` numbering from here on.
- Interfaces folded into the main package; the separate `TaskMgmt.Contracts` package was removed.
- Unit tests added; files and folders reorganized.

## [1.0.1.x] - 2026-07-06 to 2026-08-25

### Added
- `ManagedTask` gained an additional iteration runtime, `StopIteratingOnException`, and multi-iteration `GetRuntime` support.

### Changed
- Iteration system overhauled (an initial pass, then a further rework).
- `ManagedTask`/`IManagedTask` replaced with a `ManagedTask` model that includes a handle for runtime items.

### Fixed
- Scheduler timing issue (released as `1.0.1.1` on 2026-07-09).

## [1.0.0.x] - 2026-06-05 to 2026-06-25

### Added
- First versioned release (`1.0.0.1`).

### Changed
- `TaskManagement.Scheduling` combined back into `TaskManagement`.
- `AndrewM5.DevKit` namespace renamed to `Integration.DevKit`.
- Host and service-collection setup combined into a single `Service_TaskMgmt` class.

## Pre-1.0 (unversioned) - 2026-01-28 to 2026-06-04

### Added
- Started as the **Threading** module, with scheduling split into its own module (2026-01-30).

### Changed
- Threading module renamed to **TaskManagement** and reworked to use a registry (2026-02-07).
- Calls the Core garbage-collector helper.
- `TaskManagement.Scheduling` folded back into `TaskManagement` (2026-02-25).
- Reworked twice, including an inner retry system for iterations (2026-03-20, 2026-03-21).
