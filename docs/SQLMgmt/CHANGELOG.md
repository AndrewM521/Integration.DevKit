# Changelog — Integration.DevKit.SQLMgmt

This changelog covers only the `Integration.DevKit.SQLMgmt` package. See the [root changelog](../CHANGELOG.md) for the full SDK release notes.

## [2.1.1.0] - 2026-10-30 (current)

### Added
- The NuGet package now includes this module's README (`docs/SQLMgmt/README.md`), shown on the package's nuget.org page.

### Changed
- Version bumped from `2.1.0.0` to `2.1.1.0`.

## [2.1.0.0] - 2026-10-28

### Changed
- Now multi-targets `net8.0`, `net9.0` and `net10.0` (previously `net8.0` only). Building all three targets requires the .NET 10 SDK.
- `Microsoft.Data.SqlClient` bumped to `6.1.8`; `Microsoft.Extensions.*` packages bumped to `10.0.14`.

### Notes
- Behavior from the `Microsoft.Extensions.*` 10.x packages applies on every target: configuration binding keeps JSON `null` as `null` instead of `""`.

## [2.0.0.0] - 2026-09-18

### Changed
- Version bumped from `1.2.0.0-preview` to `2.0.0.0`; strict `Major.Minor.Build.0` numbering from here on.
- Interfaces folded into the main package; the separate `SQLMgmt.Contracts` package was removed.
- Unit tests added; files and folders reorganized.
- NuGet package metadata added.

## [1.0.1.x] - 2026-07-06 to 2026-08-25

### Added
- Clients are configured with a single connection string instead of individual connection parameters.

### Changed
- Renamed to include "Client" in its names; namespaces and usings updated.

### Fixed
- Small SQL bug.

## [1.0.0.x] - 2026-06-05 to 2026-06-25

### Added
- First versioned release (`1.0.0.1`).

### Changed
- SQL client uses a callback for SQL parameters instead of an array.
- Module renamed again; renamed from "Management" to "Mgmt" in namespaces and NuGet metadata.
- `AndrewM5.DevKit` namespace renamed to `Integration.DevKit`.
- Host and service-collection setup combined into a single `Service_SQLMgmt` class.

### Fixed
- Internal SQL manager constructor issue.

## Pre-1.0 (unversioned) - 2026-01-28 to 2026-06-04

### Added
- SQL Management module added (2026-02-12).
- Secret store support added to the SQL client (2026-02-17).

### Changed
- Module renamed (2026-05-08).

### Fixed
- SQL client fixes.
