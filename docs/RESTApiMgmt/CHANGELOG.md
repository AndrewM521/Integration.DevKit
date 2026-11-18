# Changelog — Integration.DevKit.RESTApiMgmt

This changelog covers only the `Integration.DevKit.RESTApiMgmt` package. See the [root changelog](../CHANGELOG.md) for the full SDK release notes.

## [3.0.0.0] - 2026-11-16 (current)

### Removed
- **Breaking:** `ApiClient.LogRuntimeSettings` and `ApiManager.LogRuntimeSettings`. They wrote every setting to the logs at Trace level, including default header values such as `Authorization` tokens and API keys.
- **Breaking:** `ApiClientSettings.Username` and `ApiClientSettings.Password`. `ApiClient` never read them; they predate `IAuthStrategy`. Pass credentials to your `IAuthStrategy` instead, ideally read from a secret store such as [Credential Management](../CredentialMgmt/README.md). Leftover `Username`/`Password` keys in `appsettings.json` are ignored by configuration binding, but should be deleted so plain-text credentials don't linger in config.

### Fixed
- `MaxConcurrentRequests = 0` made a semaphore with no slots, so every request waited forever. Zero now means no limit, the same as negative values.
- Calling `ApiClient.Initialize()` again after the client had sent a request always failed, because `HttpClient` rejects `BaseAddress`/`Timeout` writes after its first request, even with the same value. `Initialize()` now works at any point in the app's lifetime: it updates the `HttpClient` in place when it can, and when `BaseUrl` or `HttpTimeout_Seconds` changed after the first request it switches to a new `HttpClient` from `IHttpClientFactory`, carrying over every default header. Requests already in flight finish on the old one.
- Headers removed from `RuntimeSettings.DefaultHeaders` kept being sent after `Initialize()`, because it only added or updated headers. They are now removed. Headers added directly with `AddDefaultHeader` are left alone.
- Two concurrent `Initialize()` calls could interleave. They are now serialized.
- `Initialize()` no longer disposes the old rate limiter while requests are still using it. In-flight requests release the limiter they started with.
- The `Authorization` header branches for a bare `basic`/`bearer` value threw and were swallowed; they are removed. `Scheme token` values are parsed as before.
- Setup error messages named `AddApiManagement()`, which doesn't exist; they now name `AddRESTApiMgmt()`.
- The sync methods (`Get`, `Post`, `Put`, `Delete`, `ApiEndpoint.BuildQueryUrl`) could hang forever when called from a UI thread (WinForms/WPF) or classic ASP.NET, because the async code waited to resume on the blocked thread. The async code now uses `ConfigureAwait(false)`.
- On a successful call, `ApiOperationResult.ResponseBody` held the summary text (`"HTTP 200 OK"`) instead of the response body, and `DisplaySummary` was just `"Success"`. Both now hold what their names say.
- A client whose settings couldn't be applied (an invalid `BaseUrl`, or a negative `Default_HttpTimeout_Seconds`) was still handed out without its rate limiter, so its first request threw `NullReferenceException`. `GetClient` now throws the real error.
- A configured default header that `HttpClient` refuses (e.g. `Content-Type`, which belongs on the request content) was dropped silently. It's still skipped, and now logged as a warning.
- `ApiEndpoint.BuildQueryUrl` on a route that already has a query (`search?type=all`) added a second `?`. It now adds the parameters with `&`.
- `ApiClient.Initialize()` after the first request edited the default headers of the `HttpClient` that requests were being sent on, which is not thread-safe: a request sent at the same moment could fail or go out without a header. When the base address, timeout or settings headers change, `Initialize()` now builds a complete new `HttpClient` and only then swaps it in; when none of them changed it keeps the current one. `AddDefaultHeader` still edits the live client, so call it before the client is in use.

### Changed
- **Behavior change:** `ApiEndpoint.BuildSlashUrl`/`BuildPositionalUrl` now encode path segments with `Uri.EscapeDataString`. A space became `+` before, which servers read as a literal plus sign in a path; it is now `%20`.
- **Behavior change:** `ApiEndpoint` formats URL values with the invariant culture, and dates as ISO 8601 round-trip (`"o"`). Before, `1.5` became `1,5` and dates used the machine's local format on non-US servers.
- **Behavior change:** per-request headers that can't be applied (e.g. an invalid name, or `Content-Type` on a request with no body) now fail the call with `400 BadRequest` and the request is not sent. Previously they were silently dropped and the request went out without them.
- **Behavior change:** a request that times out (or is canceled) now returns `408 RequestTimeout` ("Request timed out or was canceled") instead of `500 InternalServerError` ("Unexpected execution error").
- **Breaking:** `ApiManager.GetClient` now throws `KeyNotFoundException` for a client name that isn't configured. It used to fall back to default settings, whose `BaseUrl` is `https://example.com`, so a typo sent requests (and any credentials an auth strategy added) to that third-party host.
- Client names in `ApiManagerSettings.Clients` are now matched case-insensitively, like the client cache. Before, `GetClient("testclient")` with config `TestClient` got default settings, and that misconfigured client was then cached for `TestClient` too.
- `ApiRequest.GetAsync`/`PostAsync`/`PutAsync`/`DeleteAsync` now share one helper that builds and sends the request. No behavior change.
- **Behavior change:** a negative `Default_HttpTimeout_Seconds` now means no timeout, the same rule as a client's own `HttpTimeout_Seconds`. Before, it became `-1`, which broke every client that used the default.
- The package no longer references `Integration.DevKit.CredentialMgmt.Contracts`; it hasn't used it since `Username`/`Password` were removed.
- Errors passed up inside the module keep their original stack trace (see the Core changelog).
- **Behavior change:** `ApiEndpoint.BuildQueryUrl`/`BuildQueryUrlAsync` now encode query keys and values with `Uri.EscapeDataString` (RFC 3986), like the path builders: a space becomes `%20` instead of `+`. `+` as a space is only an HTML-form convention, so strict servers read it as a literal plus sign, and signed requests (e.g. AWS Signature V4, OAuth 1.0) failed their signature check. `BuildQueryUrl` also no longer blocks on an async call to build the query.
- Version bumped from `2.1.1.0` to `3.0.0.0`. Major version bump because of the removed API and behavior changes above.

## [2.1.1.0] - 2026-10-30

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
