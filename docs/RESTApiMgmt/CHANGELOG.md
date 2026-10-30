# Changelog — Integration.DevKit.RESTApiMgmt

This changelog covers only the `Integration.DevKit.RESTApiMgmt` package. See the [root changelog](../CHANGELOG.md) for the full SDK release notes.

## [2.1.1.0] - 2026-10-30 (current)

### Added
- The NuGet package now includes this module's README (`docs/RESTApiMgmt/README.md`), shown on the package's nuget.org page.

### Changed
- Version bumped from `2.1.0.0` to `2.1.1.0`.

## [2.1.0.0] - 2026-10-28

### Changed
- Now multi-targets `net8.0`, `net9.0` and `net10.0` (previously `net8.0` only). Building all three targets requires the .NET 10 SDK.
- `Microsoft.Extensions.*` packages (including `Microsoft.Extensions.Http`) bumped to `10.0.14`.

### Notes
- Behavior from the `Microsoft.Extensions.*` 10.x packages applies on every target: configuration binding keeps JSON `null` as `null` instead of `""`, and `IHttpClientFactory` uses `SocketsHttpHandler` and redacts header values in its logs.

## [2.0.0.0] - 2026-09-18

### Added
- `IAuthStrategy` extension point for applying auth to outgoing requests.
- `IAuthStrategy.ApplyAsync` receives an `IAuthClient` for calling the authentication endpoint (e.g. an OAuth2 token endpoint). Calls made through it never re-enter the strategy, bypass the owning client's concurrency limit and metrics, and work for an endpoint on the same host (relative URL) or a different host (absolute URL, sent without the client's default headers).
- `ApiClient.Initialize()` to re-apply runtime settings after changing them.
- `GET`/`DELETE` requests can now send body content.
- NuGet package metadata added.

### Changed
- Version bumped from `1.2.0.0-preview` to `2.0.0.0`; strict `Major.Minor.Build.0` numbering from here on.
- Interfaces folded into the main package; the separate `RESTApiMgmt.Contracts` package was removed.
- Unit tests added; files and folders reorganized.

## [1.0.1.x] - 2026-07-06 to 2026-08-25

### Changed
- Renamed to include "Client" in its names; namespaces and usings updated.

### Removed
- Standalone OAuth module removed (2026-08-31); auth is handled through `IAuthStrategy` instead.

## [1.0.0.x] - 2026-06-05 to 2026-06-25

### Added
- First versioned release (`1.0.0.1`).

### Changed
- Renamed from "Management" to "Mgmt" in namespaces and NuGet metadata.
- `AndrewM5.DevKit` namespace renamed to `Integration.DevKit`.
- Host and service-collection setup combined into a single `Service_RESTApiMgmt` class.

## Pre-1.0 (unversioned) - 2026-01-28 to 2026-06-04

### Added
- API Management module added (2026-02-11).
- Secret store support added to `ApiClient` (2026-02-17).

### Changed
- Renamed and moved to `ApiClientManagement`; NuGet packages consolidated into `Directory.Build.props` (2026-05-19).

### Fixed
- Small `ApiClient` check fixed.
