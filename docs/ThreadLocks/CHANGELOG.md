# Changelog — Integration.DevKit.ThreadLocks

SDK-wide changes are summarized here when they affect this package. See the [root changelog](../CHANGELOG.md) for the full SDK release notes.

## [2.1.0.0] - 2026-10-28 (current)

### Changed
- Now multi-targets `net8.0`, `net9.0` and `net10.0` (previously `net8.0` only). Building all three targets requires the .NET 10 SDK.
- `Microsoft.Extensions.*` packages bumped to `10.0.14`.

## [2.0.0.0] - 2026-09-18

### Changed
- Version bumped from `1.2.0.0-preview` to `2.0.0.0`; strict `Major.Minor.Build.0` numbering from here on.
- Interfaces folded into the main package; the separate `ThreadLocks.Contracts` package was removed.
- Unit tests added; files and folders reorganized.
- NuGet package metadata added.

## [1.0.0.x] - 2026-06-05 to 2026-06-25

### Added
- First versioned release (`1.0.0.1`).

### Changed
- `AndrewM5.DevKit` namespace renamed to `Integration.DevKit`.
- Host and service-collection setup combined into a single `Service_ThreadLocks` class.

## Pre-1.0 (unversioned) - 2026-01-28 to 2026-06-04

### Added
- Thread Lock module added (2026-02-02).
