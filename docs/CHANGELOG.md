# Changelog

This is the SDK-wide changelog. Each module also keeps its own changelog with only the changes that affect that package:

| Module | Changelog |
| --- | --- |
| Core | [Core/CHANGELOG.md](Core/CHANGELOG.md) |
| REST API Management | [RESTApiMgmt/CHANGELOG.md](RESTApiMgmt/CHANGELOG.md) |
| SQL Management | [SQLMgmt/CHANGELOG.md](SQLMgmt/CHANGELOG.md) |
| Task Management | [TaskMgmt/CHANGELOG.md](TaskMgmt/CHANGELOG.md) |
| Process Launcher | [ProcessLauncher/CHANGELOG.md](ProcessLauncher/CHANGELOG.md) |
| Thread Locks | [ThreadLocks/CHANGELOG.md](ThreadLocks/CHANGELOG.md) |
| Thread-Safe File I/O | [ThreadSafeItems/CHANGELOG.md](ThreadSafeItems/CHANGELOG.md) |
| Credential Management | [CredentialMgmt/CHANGELOG.md](CredentialMgmt/CHANGELOG.md) |
| Credential Management Contracts | [CredentialMgmt.Contracts/CHANGELOG.md](CredentialMgmt.Contracts/CHANGELOG.md) |

Going forward, record module-specific changes in that module's changelog, and add an entry here only for releases or changes that span the SDK (version bumps, target frameworks, shared dependencies, build tooling). History for removed modules (CustomLogger/Logging, OAuth) lives only in this file.

## [3.0.0.0] - 2026-11-16 (current)

### Removed
- **Breaking:** Core's configuration protection (`EncryptJsonOnBuild`/`DecryptJsonOnBuild` and the protector classes), `LogRuntimeSettings` in REST API, SQL and Task Management, the unused `ApiClientSettings.Username`/`Password`, and `TaskManager.AwaitAllTasksToFinish` (use `Task.WhenAll`). See each module's changelog.
- **Breaking:** the `allowNoFileExtension` parameter of `FileUtils.DeleteFiles`, which was ignored. See the Core changelog.

### Fixed
- Concurrency fixes in Thread Locks, Thread-Safe File I/O, SQL Management (single-connection mode) and Task Management, plus `FileUtils` empty writes and SQL CSV export. Process Launcher no longer leaves fast-exiting processes registered; Credential Management's `CompositeSecretReader` keeps each reader's error; Task Management's hourly/interval schedules no longer drift at daylight-saving changes; REST API Management fixes `MaxConcurrentRequests = 0` and repeat `Initialize()` calls. Task Management no longer keeps every finished iteration in memory, starts a fresh snapshot when a key is run again, no longer empties its registry at the size limit, no longer loses track of a running task when the same key is started twice, no longer skips the first run of a schedule whose start time is ahead, and caps the exceptions it keeps. Thread-Safe File I/O uses one lock per file however the path is spelled. See each module's changelog.
- REST API: successful calls report the real `ResponseBody`, and a client with unusable settings fails at `GetClient` instead of crashing on its first request. Core: `OnDemandHost` stops on `StopApplication()`, and `DictionaryUtils` no longer throws on a null in a value-type list.
- Task Management: a parallel task with no iteration limit no longer hangs `StartTask` and fills memory. Credential Management: secret writes are flushed to disk before they replace the old file. REST API: `Initialize()` no longer edits headers on a client that is sending requests. Process Launcher: a failed cancel no longer loses track of a running process. SQL: CSV export uses the invariant culture and ISO 8601 dates. Core: tiny exponent numbers in JSON are no longer read as `0`, and existing folders with a dot in their name (e.g. `v1.2`) count as folders. See each module's changelog.

### Behavior changes
- REST API: unappliable request headers now fail the call (`400`) instead of being dropped, and timeouts return `408` instead of `500`. `ApiEndpoint` encodes spaces as `%20` in paths and query strings (RFC 3986; query strings used `+`) and formats values with the invariant culture.
- REST API and SQL: `GetClient` throws for an unconfigured client name instead of falling back to defaults, and names are matched case-insensitively.
- Task Management: `ManagedTaskSnapshot.IterationHistory` is replaced by `GetIterationHistory()`, which returns a safe copy.
- Process Launcher: `EnableProcessLogging` and `AutoRestartOnFailure` removed; output is capped by the new `MaxOutputLines`; processes are canceled on app shutdown.
- Core: `OnDemandHost` is now an instance class that can be restarted; `OnDemand_Registry` is removed. JSON numbers that aren't integers become `decimal` (so amounts keep every digit), except values too small for `decimal`, which stay `double`. Value conversions in Core, SQL and Credential Management use the invariant culture.
- Task Management: `TimeStrategy_Interval` rejects zero or negative intervals. Retries now back off (1s, 2s, 4s ... up to 60s) instead of running every 50 ms. `StartTask` rejects a time strategy instance already used by another running task.
- Process Launcher: a graceful cancel now really waits up to 3 seconds before killing; before, it always force killed at once.
- REST API and SQL: the sync methods no longer deadlock when called from a UI thread or classic ASP.NET.
- Core: `OnDemandHost` starts hosted services one by one in registration order and stops them in reverse, like the generic host.
- SQL: the secret store container is renamed from `ApiClient(<name>)` to `SQLClient(<name>)`; stored connection strings must be saved again.
- REST API: a negative `Default_HttpTimeout_Seconds` means no timeout; `GetClient` throws for settings it can't apply.
- Core: `JsonUtils` rejects trailing commas everywhere.
- SQL: a connection string from the secret store is read once and reused until the store is set again. **Breaking:** `RunDataReader(sql:)` is now `RunDataReader(sqlStatement:)`.
- Task Management: `StartTask` rejects a parallel task with no concurrency limit, no iteration limit and no iteration strategy.
- Credential Management: relative `SecretsFolder`/`KeysFolder` paths resolve against the app's folder, not the working directory.

### Changed
- Core, RESTApiMgmt, SQLMgmt, TaskMgmt and ProcessLauncher bumped to `3.0.0.0`; ThreadLocks, CredentialMgmt and CredentialMgmt.Contracts to `2.1.2.0`; ThreadSafeItems to `2.3.0.0`.
- New test projects `tests/Integration.DevKit.ProcessLauncher.Tests` and `tests/Integration.DevKit.ThreadSafeItems.Tests`.
- Sample `TestApp` writes its scratch files, secrets, keys and logs to a `Junk` folder next to the app (relative paths, resolved against the app's folder) instead of hard-coded folders on one machine; `Junk/` is git-ignored. The unused `RnadomManagement` config section is removed.
- Sample `TestApp` now reads plain `appsettings.json`; its encrypted sample values were replaced with their plain text.
- Errors rethrown inside any module keep their original stack trace.
- Each package references only the `Microsoft.Extensions.*` packages it uses; `Integration.DevKit.Core` no longer brings `Options`/`Options.ConfigurationExtensions`.

## [2.1.1.0] - 2026-10-30

### Added
- Every module's NuGet package now includes its README from `docs/<Module>/README.md`. Packing is set up once in `src/Directory.Build.targets` and turned on per project with `<DevKitDocsFolder>` in the `.csproj`.

### Docs
- Docs reorganized into one folder per module under `docs/`, each with its own `README.md` and `CHANGELOG.md`. Core and ThreadSafeItems also have a page per group of classes.

### Changed
- Module versions bumped from `2.1.0.0` to `2.1.1.0`, except ThreadSafeItems, which goes to `2.2.0.0` for a breaking rename (see its [changelog](ThreadSafeItems/CHANGELOG.md)).

## [2.1.0.0] - 2026-10-28

### Changed
- All modules now multi-target `net8.0`, `net9.0` and `net10.0`. Building all three targets requires the .NET 10 SDK (CLI or Visual Studio 2026); Visual Studio 2022 cannot use that SDK, so it builds `net8.0` and `net9.0` only.
- All module versions bumped to `2.1.0.0`.
- `Microsoft.Extensions.*` packages bumped to `10.0.14`; `Microsoft.Data.SqlClient` bumped to `6.1.8`.
- Test tooling bumped: `Microsoft.NET.Test.Sdk` 18.11.0, `xunit` 2.9.3, `xunit.runner.visualstudio` 3.1.5, `coverlet.collector` 10.1.0.
- Sample `TestApp` now targets `net10.0`.

### Security
- Credential Management: `Microsoft.AspNetCore.DataProtection` was `10.0.3`, which is affected by CVE-2026-40372. It now references a version matching each target: `8.0.33` (net8.0), `9.0.22` (net9.0), `10.0.14` (net10.0). This also stops the net8.0 build pulling DataProtection 10 into ASP.NET Core 8/9 apps. If an affected build ran somewhere internet-facing, consider rotating the DataProtection key ring.

### Notes
- Behavior changes from the `Microsoft.Extensions.*` 10.x packages apply on every target (they were already in effect in 2.0.x): configuration binding keeps JSON `null` as `null` instead of `""`, and `IHttpClientFactory` uses `SocketsHttpHandler` and redacts header values in its logs.

## [2.0.0.0] - 2026-09-18

### Versioning
- Switched version numbering scheme to strict `Major.Minor.Build.0` format going forward.
- 8 modules (Core, REST API Management, SQL Management, Task Management, Process Launcher, Thread Locks, Thread-Safe File I/O, Credential Management) bumped from `1.2.0.0-preview` to `2.0.0.0`, promoting the 1.2 preview content below to a final release.

### Changed
- 8 current modules (Core, REST API Management, SQL Management, Task Management, Process Launcher, Thread Locks, Thread-Safe File I/O, Credential Management) bumped together to `1.2.0.0-preview`.
- Added unit tests across modules and expanded setup code into separate classes for readability.
- Major cleanup and reorganization of files/folders.
- Fixed a small bug in JSON key normalization.

### Removed
- **CustomLogger (formerly Logging)** module removed entirely.
- Most `.Contracts` projects were removed as part of the cleanup — `CredentialMgmt.Contracts` is now the only surviving `.Contracts` package.

### Added
- REST API Management gained an `IAuthStrategy` extension point for applying auth to outgoing requests.
- `IAuthStrategy.ApplyAsync` now receives an `IAuthClient` for calling the authentication endpoint (e.g. an OAuth2 token endpoint). Calls made through it never re-enter the strategy, bypass the owning client's concurrency limit and metrics (so a strategy that used the same `ApiClient` could otherwise recurse or deadlock), and work for an endpoint on the same host (relative URL) or a different host (absolute URL, sent without the client's default headers).
- NuGet package references and metadata added to each module.
- `Initialize` methods added to Task Management and the REST API client to allow re-initializing runtime settings.

### Fixed
- Process Launcher timeout issue.
- `LogFileRegistry` now enforces a max registry size when not creating files.

### Docs
- Updated REST API docs; REST API Management now allows `GET`/`DELETE` to send body content.

## [1.0.1.x] - 2026-07-06 to 2026-08-25

Modules moved from `1.0.1.0` up through `1.0.1.2`–`1.0.1.5` over this stretch, at slightly different paces per module.

### Added
- SQL Management: connection string setting instead of individual connection params.
- `JsonUtil` overhauled to be much more generic, with JSON encrypt/decrypt support; keys containing `.` are now normalized, with an option to always create the root structure.
- Services gained on-demand initialization; utility methods reshuffled between `DictionaryUtil` and `FileUtil`, including a new `ReadBytes` helper.
- `ManagedTask` gained an additional iteration runtime, `StopIteratingOnException`, and multi-iteration `GetRuntime` support.

### Changed
- On-demand initialization moved out of each module into its own custom host.
- Task Management iteration system overhauled (twice — an initial pass, then a further rework).
- API and SQL modules renamed to include "Client" in their names; namespaces/usings updated to match.

### Removed
- **CustomLogger and OAuth** (as a standalone module) removed on 2026-08-31 — CustomLogger had reached version `1.0.1.4` by 2026-08-25, just before removal.
- Unused folders removed from the remaining Contract modules.

### Fixed
- Task Management: fixed a scheduler timing issue (Task Management reached `1.0.1.1` on 2026-07-09, one point release ahead of the other modules).
- Small SQL bug fixed alongside a `ManagedTask`/`IManagedTask` rework (replaced with a `ManagedTask` model that includes a handle for runtime items).

## [1.0.0.x] - 2026-06-05 to 2026-06-25

First versioned release. All 8 modules (plus CustomLogger, still active at this point) started at `1.0.0.1` on 2026-06-05.

### Added
- MIT license added; license headers applied to every file in the project.
- Versions added to every module for the first time.

### Changed
- SQL Client updated to use a callback for SQL params instead of an array.
- `TaskManagement.Scheduling` combined back into `TaskManagement`.
- Extensions renamed/modified to `Utils`; fixed a bug in `NullableOperationResult`.
- SQL module renamed (again) and further documentation work started.
- `AndrewM5.DevKit` namespace renamed to `Integration.DevKit` project-wide; `appsettings` lookup/structure updated to match.
- Namespaces changed to use the root namespace instead of a per-folder namespace (done manually).
- API/SQL modules renamed from "Management" to "Mgmt" throughout namespaces and NuGet metadata.
- Host and Service Collection combined into a single Service class per module.

### Fixed
- Removed the dependency on a separate logger manager and fixed an internal SQL manager constructor issue.

## Pre-1.0 (unversioned) - 2026-01-28 to 2026-06-04

No module had a `<Version>` yet. This period covers the initial build-out of the SDK's modules and several renames/mergers along the way.

### Added
- Initial modules and core classes; **Logging** module added with its own logger classes (2026-01-29).
- **Threading** module added, with scheduling initially split out into its own module (2026-01-30).
- Thread Lock module added (2026-02-02).
- Garbage collector added; Task Management updated to call it.
- API Management module added (2026-02-11).
- SQL Management module added, "still needs to be tested" (2026-02-12).
- Credential Management module added; file/directory extensions improved to verify strings (2026-02-17).
- Secret store support added to `SqlDBClient` and `ApiClient` (2026-02-17).
- Thread-safe items module added (2026-02-09).
- **OAuth module added as an early work-in-progress** (2026-03-27).
- Test app included in the solution.

### Changed
- Logging interfaces removed (not needed with DI); logging abstractions later moved to their own module so all modules can use logging interfaces without depending on the Logging module directly.
- Older `LoggerManager` removed in favor of interfaces; `LoggingServiceCollection` and DI dependencies added.
- **Threading module renamed to TaskManagement**, reworked to use a Registry (2026-02-07).
- **TaskManagement.Scheduling folded back into TaskManagement** (2026-02-25); the leftover `.csproj` was cleaned up later (2026-06-02).
- ProcessLauncher Abstractions split out of the main ProcessLauncher module, then **all per-module `.Abstractions` projects were removed and replaced with `.Contracts` projects** (2026-03-27).
- `NullOperationResult`/`NullableOperationResult` introduced across modules, with dependency lookup added for modules relying on other modules.
- SQL client and file secret store fixes; small `ApiClient` check fixed.
- Task Management reworked twice, including an inner retry system for iterations (2026-03-20, 2026-03-21).
- Logger renamed to "Customer Logger" (**Logging → CustomLogger**), moved to renamed folders, namespaces updated (2026-04-25).
- Documentation effort started: Core, Logging, API, Credential Management, Process Launcher, SQL Management, Thread-Safe + Items, and Task Management module docs were each completed in turn through April.
- API module renamed and moved to `ApiClientManagement`; nuget packages consolidated into `Directory.Build.Props` (2026-05-19).
- SQL module renamed again, with documentation ongoing (2026-05-08).
- Older docs removed and placeholders added for all modules (2026-05-06).

### Fixed
- Small bug where `CallGC_Collect` could be invoked more than once per second; fixed with a lock, and its logger call removed.
- Unused `MaxEntry` variable removed from `LogRegistry`.
