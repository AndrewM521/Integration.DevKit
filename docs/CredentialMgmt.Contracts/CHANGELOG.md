# Changelog — Integration.DevKit.CredentialMgmt.Contracts

This changelog covers only the `Integration.DevKit.CredentialMgmt.Contracts` package. See the [root changelog](../CHANGELOG.md) for the full SDK release notes.

## [2.1.1.0] - 2026-10-30 (current)

### Added
- The NuGet package now includes this module's README (`docs/CredentialMgmt.Contracts/README.md`), shown on the package's nuget.org page.

### Changed
- Version bumped from `2.1.0.0` to `2.1.1.0`.

## [2.1.0.0] - 2026-10-28

### Changed
- Now multi-targets `net8.0`, `net9.0` and `net10.0` (previously `net8.0` only). Building all three targets requires the .NET 10 SDK.
- `Microsoft.Extensions.*` packages bumped to `10.0.14`.

## [2.0.0.0] - 2026-09-18

### Changed
- Version bumped from `1.2.0.0-preview` to `2.0.0.0`; strict `Major.Minor.Build.0` numbering from here on.
- Now the only `.Contracts` package in the SDK; the other modules' `.Contracts` packages were folded into their main packages.
- NuGet package metadata added.

## [1.0.0.x] - 2026-06-05 to 2026-06-25

### Added
- First versioned release (`1.0.0.1`).

### Changed
- `AndrewM5.DevKit` namespace renamed to `Integration.DevKit`.

## Pre-1.0 (unversioned) - 2026-01-28 to 2026-06-04

### Added
- Created when per-module `.Abstractions` projects were replaced with `.Contracts` projects (2026-03-27).
