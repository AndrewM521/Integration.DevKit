# Changelog — Integration.DevKit.SQLMgmt

This changelog covers only the `Integration.DevKit.SQLMgmt` package. See the [root changelog](../CHANGELOG.md) for the full SDK release notes.

## [3.0.0.0] - 2026-11-16 (current)

### Removed
- **Breaking:** `SQLClient.LogRuntimeSettings` and `SQLManager.LogRuntimeSettings`. They wrote every setting to the logs at Trace level, including the connection string and its password.

### Fixed
- With `UseSingleConnection`, `TestSqlConnectionAsync` (and `TestSqlConnection`) never released the connection lock, so every later query on that client hung forever.
- With `UseSingleConnection`, the shared connection was fetched before taking the lock, so a second caller could dispose it while the first was waiting. All methods now take the lock first.
- `ToCsvContent`/`ToCsvContentAsync` with all columns now take header names per column, so duplicate column names (e.g. from a join) no longer drop a header and shift every later column.
- `SQLUtils.GetValueOrDefault<T>` converted with the machine's culture (e.g. `"1.5"` read as `15` on a German-locale server) and always failed for nullable `T` such as `int?`, returning the default. It now uses the invariant culture and the underlying type.
- The sync `RunCustomCommand`, `RunNonQueryCommand` and `RunDataReader` never passed `commandTimeoutSeconds` to their async versions, so every sync call used the 30-second default.
- Setup error messages named `AddSqlDBManagement()`, which doesn't exist; they now name `AddSQLMgmt()`.
- With `UseSingleConnection`, a canceled token while waiting for the connection lock threw `OperationCanceledException` out of the method instead of returning a failed result.
- The sync methods (`RunNonQueryCommand`, `RunDataReader`, `RunCustomCommand`, `TestSqlConnection`) could hang forever when called from a UI thread (WinForms/WPF) or classic ASP.NET, because the async code waited to resume on the blocked thread. The async code now uses `ConfigureAwait(false)`.
- `ToCsvContent`/`ToCsvContentAsync` didn't escape header names, so a column alias with a comma (e.g. `Total, USD`) split into two header columns and shifted the rest. Headers are now quoted like data cells.
- `ToCsvContentAsync` returned the rows read so far when cancellation was requested between rows, so a canceled export looked complete. It now throws `OperationCanceledException`.
- `ToCsvContent`/`ToCsvContentAsync` wrote cells with the machine's culture, so on a German-locale server `1234.5` was exported as `"1234,5"` and dates in the local format. Cells now use the invariant culture, and dates are written as ISO 8601 round-trip (`2026-03-04T10:00:00.0000000`, like `ApiEndpoint`), so `03/04` can no longer be read as 3 April in one place and 4 March in another.

### Changed
- **Behavior change:** `ToCsvContent`/`ToCsvContentAsync` now quote cells per RFC 4180 (wrap in `"` and double inner quotes) instead of stripping commas, quotes and line breaks. Output now round-trips; consumers that relied on cells never containing those characters must handle quoted cells.
- **Breaking:** `SQLManager.GetClient` now throws `KeyNotFoundException` for a client name that isn't configured, instead of caching a client with empty settings. Client names are matched case-insensitively.
- **Breaking:** the secret store container for a client's connection string is now `SQLClient(<name>)` instead of `ApiClient(<name>)`, a leftover from the REST client. Connection strings saved with `SetSecretStoreCredentials` under the old name are not found anymore; save them again (or copy them across with `ImportFrom`) after upgrading.
- The four async methods now share one helper for locking, opening and cleaning up the connection.
- **Behavior change:** with a secret store attached, the connection string is read from the store once and reused, instead of being read and decrypted before every query. `SetSecretStore`, `SetSecretStoreCredentials`, `DeleteCredential` and `DeleteAllCredentials` clear it. A value changed in the store by something else is picked up after calling `SetSecretStore` again.
- **Breaking:** `RunDataReader`'s first parameter is renamed from `sql` to `sqlStatement`, like every other method. Only callers passing it as the named argument `sql:` are affected.
- Errors passed up inside the module keep their original stack trace (see the Core changelog).
- `RowToDictionaryAsync` (and so `ToListDictionaryAsync`) no longer awaits `IsDBNullAsync` for every cell; the row is already in memory for normal readers, so it reads it directly.
- Version bumped from `2.1.1.0` to `3.0.0.0`. Major version bump because of the removed API and CSV behavior change above.

## [2.1.1.0] - 2026-10-30

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
