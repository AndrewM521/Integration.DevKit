# Changelog

## [1.2.0.0-preview] - 2026-09-03 (current)

### Changed
- All 8 current modules (Core, REST API Management, SQL Management, Task Management, Process Launcher, Thread Locks, Thread-Safe File I/O, Credential Management) bumped together to `1.2.0.0-preview`.
- Added unit tests across modules and expanded setup code into separate classes for readability.
- Major cleanup and reorganization of files/folders.
- Fixed a small bug in JSON key normalization.

### Removed
- **CustomLogger (formerly Logging)** module removed entirely — its files were deleted after being demoted to sample code the same week (see 1.0.1.x below for its removal date).
- Most `.Contracts` projects (REST API Management, SQL Management, Task Management, Thread Locks, Process Launcher) were removed as part of the cleanup — `CredentialMgmt.Contracts` is now the only surviving `.Contracts` package.

### Added
- OAuth support returned, not as a standalone module but as `OAuth2ClientCredentialsAuthStrategy` inside REST API Management.
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
