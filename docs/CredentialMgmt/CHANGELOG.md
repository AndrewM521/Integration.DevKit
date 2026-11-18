# Changelog — Integration.DevKit.CredentialMgmt

This changelog covers only the `Integration.DevKit.CredentialMgmt` package. See the [root changelog](../CHANGELOG.md) for the full SDK release notes.

## [2.1.2.0] - 2026-11-16 (current)

### Fixed
- `CompositeSecretReader` threw away each reader's error, so a real failure (e.g. a `CryptographicException` from a lost DataProtection key ring) was reported only as "not found". The returned `KeyNotFoundException` now carries an `AggregateException` with every reader's error as its inner exception.
- `CredentialMgmtUtils.GetRequiredOption<T>` converted with the machine's culture and failed for nullable `T`. It now uses the invariant culture and the underlying type.
- `FileSecretStore` locked on the container name as given but saved to a cleaned-up file name, so names like `a/b` and `a_b` (or `App:Prod` and `App_Prod` on Windows) wrote the same file under different locks, and parallel writes could lose each other's keys. The lock is now keyed on the file path.
- `FileSecretStore` writes were atomic but not durable: the temporary file was not flushed to disk before being moved into place, so a power cut could leave an empty `.secret` file and lose every key in that container. Writes now flush to disk first.
- README: the secret file name is `{ApplicationName}_{fileName}.secret` (it said `FileSecretStore_{fileName}.secret`), and values are stored as given (it said they were trimmed).

### Changed
- Errors passed up inside the module keep their original stack trace (see the Core changelog).
- **Behavior change:** relative `SecretsFolder` and `KeysFolder` paths are now resolved against the app's folder (`AppContext.BaseDirectory`) instead of the working directory. A Windows service starts in `C:\Windows\System32`, and launching the app another way (e.g. `dotnet run` versus the `.exe`) created a separate, empty key ring, so stored secrets could no longer be decrypted. Absolute paths are unchanged. If you used relative paths and the app was not started from its own folder, move the existing `SecretsFolder` and `KeysFolder` contents next to the app before upgrading.
- Version bumped from `2.1.1.0` to `2.1.2.0`.

## [2.1.1.0] - 2026-10-30

### Added
- The NuGet package now includes this module's README (`docs/CredentialMgmt/README.md`), shown on the package's nuget.org page.

### Changed
- Version bumped from `2.1.0.0` to `2.1.1.0`.

## [2.1.0.0] - 2026-10-28

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
