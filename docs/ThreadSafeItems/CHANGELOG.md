# Changelog — Integration.DevKit.ThreadSafeItems

This changelog covers only the `Integration.DevKit.ThreadSafeItems` package. See the [root changelog](../CHANGELOG.md) for the full SDK release notes.

## [2.3.0.0] - 2026-11-16 (current)

### Added
- The read methods (`ReadFileLines`, `ReadFileText` and their async versions) take `allowNoFileExtension`, like the write methods, so files such as `LICENSE` can be read back.

### Fixed
- When an async call timed out waiting for the file lock, it still released the lock, freeing a lock held by another caller and letting a third writer in. Calls now only release a lock they acquired.
- The file lock was keyed on the path as written, so `data\out.txt`, `.\data\out.txt` and the full path got separate locks and their writers collided on the same file. The lock is now keyed on the full path.

### Changed
- The eight `ThreadSafeFileIO` methods now share one lock-and-run helper per sync/async path.
- Failures returned by the wrapped `FileUtils` read methods are no longer logged a second time by `ThreadSafeFileIO`; they are still returned in the result.
- `AddThreadSafeItems` now registers the ThreadLocks module it needs, like `AddCredentialMgmt` does. Calling `AddThreadLocks` yourself still works.
- Version bumped from `2.2.0.0` to `2.3.0.0`. Minor version bump because the read methods gained the `allowNoFileExtension` parameter.

## [2.2.0.0] - 2026-10-30

### Added
- The NuGet package now includes this module's README (`docs/ThreadSafeItems/README.md`), shown on the package's nuget.org page.

### Changed
- Version bumped from `2.1.0.0` to `2.2.0.0`. Minor version bump because of the breaking rename below.
- **Breaking:** the static accessor `Service_ThreadSafeItems.ThreadSafeFileIOClass` was renamed to `Service_ThreadSafeItems.ThreadSafeFileIO`, matching the class name. Update call sites from `.ThreadSafeFileIOClass` to `.ThreadSafeFileIO`.

## [2.1.0.0] - 2026-10-28

### Changed
- Now multi-targets `net8.0`, `net9.0` and `net10.0` (previously `net8.0` only). Building all three targets requires the .NET 10 SDK.
- `Microsoft.Extensions.*` packages bumped to `10.0.14`.

## [2.0.0.0] - 2026-09-18

### Changed
- Version bumped from `1.2.0.0-preview` to `2.0.0.0`; strict `Major.Minor.Build.0` numbering from here on.
- Unit tests added; files and folders reorganized.
- NuGet package metadata added.

## [1.0.0.x] - 2026-06-05 to 2026-06-25

### Added
- First versioned release (`1.0.0.1`).

### Changed
- `AndrewM5.DevKit` namespace renamed to `Integration.DevKit`.
- Host and service-collection setup combined into a single `Service_ThreadSafeItems` class.

## Pre-1.0 (unversioned) - 2026-01-28 to 2026-06-04

### Added
- Thread-safe items module added (2026-02-09).
