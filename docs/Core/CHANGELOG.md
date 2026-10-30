# Changelog — Integration.DevKit.Core

This changelog covers only the `Integration.DevKit.Core` package. See the [root changelog](../CHANGELOG.md) for the full SDK release notes.

## [2.1.0.0] - 2026-10-28 (current)

### Changed
- Now multi-targets `net8.0`, `net9.0` and `net10.0` (previously `net8.0` only). Building all three targets requires the .NET 10 SDK.
- `Microsoft.Extensions.*` packages bumped to `10.0.14`.

### Notes
- Behavior from the `Microsoft.Extensions.*` 10.x packages applies on every target: configuration binding keeps JSON `null` as `null` instead of `""`.

## [2.0.0.0] - 2026-09-18

### Changed
- Version bumped from `1.2.0.0-preview` to `2.0.0.0`; strict `Major.Minor.Build.0` numbering from here on.
- Unit tests added; setup code split into separate classes; files and folders reorganized.
- NuGet package metadata added.

### Fixed
- Small bug in JSON key normalization.

## [1.0.1.x] - 2026-07-06 to 2026-08-25

### Added
- `JsonUtil` overhauled to be much more generic, with JSON encrypt/decrypt support; keys containing `.` are now normalized, with an option to always create the root structure.
- On-demand initialization for services; utility methods reshuffled between `DictionaryUtil` and `FileUtil`, including a new `ReadBytes` helper.

### Changed
- On-demand initialization moved out of each module into its own custom host (`OnDemandHost`).

## [1.0.0.x] - 2026-06-05 to 2026-06-25

### Added
- First versioned release (`1.0.0.1`). MIT license and license headers added.

### Changed
- Extensions renamed to `Utils`.
- `AndrewM5.DevKit` namespace renamed to `Integration.DevKit`; `appsettings` structure updated to match.
- Namespaces changed to use the root namespace instead of per-folder namespaces.

### Fixed
- Bug in `NullableOperationResult`.

## Pre-1.0 (unversioned) - 2026-01-28 to 2026-06-04

### Added
- Initial core classes.
- Garbage collector helper (`GCManager`).
- `NullOperationResult`/`NullableOperationResult` introduced.

### Fixed
- `CallGC_Collect` could run more than once per second; fixed with a lock and its logger call removed.
