# Changelog — Integration.DevKit.ProcessLauncher

This changelog covers only the `Integration.DevKit.ProcessLauncher` package. See the [root changelog](../CHANGELOG.md) for the full SDK release notes.

## [2.1.1.0] - 2026-10-30 (current)

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
