# Changelog — Integration.DevKit.CredentialMgmt

SDK-wide changes are summarized here when they affect this package. See the [root changelog](../CHANGELOG.md) for the full SDK release notes.

## [2.1.0.0] - 2026-10-28 (current)

### Changed
- Now multi-targets `net8.0`, `net9.0` and `net10.0` (previously `net8.0` only). Building all three targets requires the .NET 10 SDK.
- `Microsoft.Extensions.*` packages bumped to `10.0.14`.

### Security
- `Microsoft.AspNetCore.DataProtection` was `10.0.3`, which is affected by CVE-2026-40372. It now references a version matching each target: `8.0.33` (net8.0), `9.0.22` (net9.0), `10.0.14` (net10.0). This also stops the net8.0 build pulling DataProtection 10 into ASP.NET Core 8/9 apps. If an affected build ran somewhere internet-facing, consider rotating the DataProtection key ring.

### Notes
- Behavior from the `Microsoft.Extensions.*` 10.x packages applies on every target: configuration binding keeps JSON `null` as `null` instead of `""`.

## [2.0.0.0] - 2026-09-18

### Changed
- Version bumped from `1.2.0.0-preview` to `2.0.0.0`; strict `Major.Minor.Build.0` numbering from here on.
- Unit tests added; files and folders reorganized. `CredentialMgmt.Contracts` kept as the only remaining `.Contracts` package in the SDK.
- NuGet package metadata added.

## [1.0.0.x] - 2026-06-05 to 2026-06-25

### Added
- First versioned release (`1.0.0.1`).

### Changed
- `AndrewM5.DevKit` namespace renamed to `Integration.DevKit`.
- Host and service-collection setup combined into a single `Service_CredentialMgmt` class.

## Pre-1.0 (unversioned) - 2026-01-28 to 2026-06-04

### Added
- Credential Management module added (2026-02-17).

### Changed
- Per-module `.Abstractions` projects replaced with `.Contracts` projects (2026-03-27).

### Fixed
- File secret store fixes.
