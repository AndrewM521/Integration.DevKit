# Changelog — Integration.DevKit.TaskMgmt

SDK-wide changes are summarized here when they affect this package. See the [root changelog](../CHANGELOG.md) for the full SDK release notes.

## [2.1.0.0] - 2026-10-28 (current)

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
