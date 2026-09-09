# REST API Management

`Integration.DevKit.RESTApiMgmt` provides a named-client abstraction over `HttpClient`: you configure one or more clients by name (base URL, headers, timeout, credentials), resolve them through a manager, and call `GetAsync`/`PostAsync`/`PutAsync`/`DeleteAsync`, all of which return the SDK's shared `ApiOperationResult<string>` result type instead of throwing on HTTP failures.

## Requirements

- .NET 8
- `Microsoft.Extensions.Http` (for `IHttpClientFactory`)
- [Integration.DevKit.Core](core.md) (for `ApiOperationResult<T>` and the other result types)

## Installation

```bash
dotnet add reference src/Integration.DevKit.RESTApiMgmt/Integration.DevKit.RESTApiMgmt.csproj
```

Or from NuGet: [Integration.DevKit.RESTApiMgmt](https://www.nuget.org/packages/Integration.DevKit.RESTApiMgmt)

## Getting started

```csharp
using Integration.DevKit.RESTApiMgmt;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddRESTApiMgmt(configuration);
    });

var app = builder.Build();
Service_RESTApiMgmt.Initialize(app.Services);

var client = Service_RESTApiMgmt.ApiManager.GetClient("my-client");
var result = await client.GetAsync("items");

if (result.MethodSuccess)
{
    Console.WriteLine(result.Result);          // response body, deserialized as a string
}
else
{
    Console.WriteLine($"{result.StatusCode}: {result.Exception.Message}");
}
```

## Configuration

`AddRESTApiMgmt` binds the config section `Integration.DevKit:ApiClientManagement` to `ApiManagerSettings`:

```json
{
  "Integration.DevKit": {
    "ApiClientManagement": {
      "Default_HttpTimeout_Seconds": 30,
      "EnableLogging": true,
      "Clients": {
        "my-client": {
          "BaseUrl": "https://api.example.com/",
          "MaxConcurrentRequests": 10,
          "HttpTimeout_Seconds": 15,
          "DefaultHeaders": {
            "Accept": "application/json"
          }
        }
      }
    }
  }
}
```

`EnableLogging` (default `true`) is checked fresh on every log call rather than only at startup — flip it at runtime via `apiManager.RuntimeSettings.EnableLogging = false;` to silence this module's logging (including every `ApiClient` it hands out) without detaching the `ILoggerFactory` you registered for the rest of the app.

### `ApiClientSettings` (per named client)

| Property | Default | Notes |
| --- | --- | --- |
| `BaseUrl` | `"https://example.com"` | Relative URLs passed to `GetAsync`/etc. are resolved against this. |
| `MaxConcurrentRequests` | `int.MaxValue` | Negative values are coerced to `int.MaxValue`. |
| `HttpTimeout_Seconds` | `null` | `null` falls back to the manager's `Default_HttpTimeout_Seconds`. **`0` means no timeout** (mapped to `Timeout.InfiniteTimeSpan`), not "time out immediately" — negative values are coerced to `0`. |
| `DefaultHeaders` | `{}` | Applied to every request from this client. |

All named clients share a single `IHttpClientFactory`-managed connection pool internally (they're all created via the fixed factory name `"ApiClient"`); per-client `BaseUrl`, headers, and timeout are applied on top of that shared handler when each `ApiClient` is constructed.

## Making requests

`ApiClient`, `ApiManager`, and `ApiClientMetrics` are plain classes directly under `Integration.DevKit.RESTApiMgmt`. The module's only real extension point is `IAuthStrategy`, in `Integration.DevKit.RESTApiMgmt.Interfaces`; `ApiClientSettings`/`ApiManagerSettings` live in `Integration.DevKit.RESTApiMgmt.Settings`. See [Extending DevKit modules](extending-modules.md) for the general pattern.

```csharp
public class ApiClient : IAsyncDisposable
{
    ApiClientSettings RuntimeSettings { get; }
    ApiClientMetrics ClientMetrics { get; }
    string ClientName { get; }

    NullOperationResult Initialize();

    Task<ApiOperationResult<string>> GetAsync(string endpointUrl, HttpContent? httpContent = null, Dictionary<string, string>? requestHeaders = null);
    Task<ApiOperationResult<string>> PostAsync(string endpointUrl, HttpContent? httpContent = null, Dictionary<string, string>? requestHeaders = null);
    Task<ApiOperationResult<string>> PutAsync(string endpointUrl, HttpContent httpContent, Dictionary<string, string>? requestHeaders = null);
    Task<ApiOperationResult<string>> DeleteAsync(string endpointUrl, HttpContent? httpContent = null, Dictionary<string, string>? requestHeaders = null);

    // Get / Post / Put / Delete: synchronous equivalents of the four methods above

    OperationResult<HttpContent> CreateHttpContent(RESTApiMediaTypes mediaType, string data, Encoding? encoding = null);
    NullOperationResult AddDefaultHeader(string key, string value);
    void LogRuntimeSettings(bool calledFromManager = false);

    void SetAuthStrategy(IAuthStrategy? authStrategy);
}
```

### Re-initializing after mutating `RuntimeSettings`

`ApiClient` caches several things derived from `RuntimeSettings` at construction time: the underlying `HttpClient`'s base address, timeout, and default headers, plus a `SemaphoreSlim` sized from `MaxConcurrentRequests`. Mutating `RuntimeSettings` in place (e.g. `client.RuntimeSettings.BaseUrl = "..."`) has no effect until you call `Initialize()`, which re-derives all of it:

```csharp
var client = Service_RESTApiMgmt.ApiManager.GetClient("my-client");
client.RuntimeSettings.BaseUrl = "https://api2.example.com/";
client.RuntimeSettings.MaxConcurrentRequests = 20;
client.Initialize();
```

Requests already waiting on the old rate limiter when `Initialize()` runs will see it disposed out from under them — prefer calling this during a quiet period rather than under active load.

`GetAsync` and `DeleteAsync` both accept an optional `HttpContent` body — this sets an actual `HttpContent` on the underlying `HttpRequestMessage` for a GET/DELETE request. That's non-standard HTTP usage (most servers ignore a GET body, and some client stacks strip it), so only rely on it against an API you know supports it.

```csharp
var payload = client.CreateHttpContent(RESTApiMediaTypes.Json, "{\"id\":42}");
var result = await client.PostAsync("orders", payload.Result);
```

`RESTApiMediaTypes` is `Json | Xml | PlainText | WWW_UrlEncoded`, mapping to the corresponding standard MIME type.

### Authentication via `IAuthStrategy`

For downstream APIs that require an OAuth2 bearer token, a static API key, or any other credential scheme, attach an `IAuthStrategy`:

```csharp
public interface IAuthStrategy
{
    Task<NullOperationResult> ApplyAsync(HttpRequestMessage request);
}
```

`SetAuthStrategy` is the only credential/auth mechanism `ApiClient` has — there's no separate Basic-auth or secret-store attachment on the client itself; everything goes through an `IAuthStrategy`. When set, the strategy runs on every outgoing request (`GetAsync`/`PostAsync`/`PutAsync`/`DeleteAsync`, sync or async) just before it's sent, and any failure it returns short-circuits the call as a failed `ApiOperationResult<string>` (HTTP 401) rather than hitting the network.

This module ships **no built-in `IAuthStrategy` implementation** — `IAuthStrategy` is a pure extension point, and you're expected to write your own (an OAuth2 client-credentials flow, a static API key, a signed-request scheme, mTLS header injection). If your downstream API needs OAuth2, compose your own strategy with the [Credential Management](credential-management.md) module's `ISecretReader`/`ISecretStore` to source the client secret.

### Implementing your own `IAuthStrategy`

See [Extending DevKit modules](extending-modules.md) for how a custom `IAuthStrategy` fits into the module's `Interfaces/`/`Settings/` layout in general. A minimal example:

```csharp
using Integration.DevKit.Core;
using Integration.DevKit.RESTApiMgmt.Interfaces;

public class ApiKeyAuthStrategy : IAuthStrategy
{
    private readonly string _headerName;
    private readonly string _apiKey;

    public ApiKeyAuthStrategy(string headerName, string apiKey)
    {
        _headerName = headerName;
        _apiKey = apiKey;
    }

    public Task<NullOperationResult> ApplyAsync(HttpRequestMessage request)
    {
        request.Headers.Add(_headerName, _apiKey);
        return Task.FromResult(new NullOperationResult().SetMethodSuccess());
    }
}
```

```csharp
client.SetAuthStrategy(new ApiKeyAuthStrategy("X-Api-Key", apiKey));
```

There's no requirement about where a custom strategy like this lives — `Implementations/` is this library's own internal convention for the classes it ships, not something imposed on consumers. Put `ApiKeyAuthStrategy` wherever the rest of your application's code lives.

## Client metrics

```csharp
public class ApiClientMetrics
{
    int TotalRequests { get; }   // GetCount + PostCount + PutCount + DeleteCount + OtherCount
    int SuccessCount { get; }    // TotalRequests - FailureCount
    int FailureCount { get; }
    int GetCount { get; }
    int PostCount { get; }
    int PutCount { get; }
    int DeleteCount { get; }
    int OtherCount { get; }      // includes PATCH/HEAD/OPTIONS — there is no dedicated counter for those
}
```

Access via `client.ClientMetrics`; counters are read-only from the outside and update automatically on every call made through that client.

## `ApiManager`

```csharp
public class ApiManager : IAsyncDisposable
{
    ApiManagerSettings RuntimeSettings { get; set; }
    ApiClient GetClient(string clientName);
    void LogRuntimeSettings();
}
```

`GetClient` caches and reuses one `ApiClient` per name (case-insensitive) — calling it twice with the same name returns the same instance. If `clientName` isn't found in the configured `Clients` dictionary, it logs a warning and hands back a client built from default `ApiClientSettings` rather than throwing, so a typo in a client name fails silently at the HTTP layer (wrong base URL) rather than at `GetClient` — double-check the name against your configuration if requests are going to the wrong host.

`ApiManager` is a plain concrete class with no interface — nothing in this SDK substitutes or mocks a different `ApiManager` implementation, so an interface would be pure ceremony. See [Extending DevKit modules](extending-modules.md).

## API Reference

### `Service_RESTApiMgmt` (static)

```csharp
public static IServiceCollection AddRESTApiMgmt(this IServiceCollection services, IConfiguration configuration);
public static void Initialize(IServiceProvider sp);
public static ApiManager ApiManager { get; }   // throws InvalidOperationException before Initialize
```

### `ApiOperationResult<T>` (from [Integration.DevKit.Core](core.md#result-types))

```csharp
public string RequestUrl { get; }
public HttpStatusCode StatusCode { get; }
public string? ResponseBody { get; }
public string? DisplaySummary { get; }
// plus MethodSuccess, Result, Exception inherited from NullableOperationResult<T>
```

Every REST call in this module returns `ApiOperationResult<string>` — there is no built-in JSON-deserializing overload. Deserialize `Result`/`ResponseBody` yourself (e.g. with `JsonUtils` from Core, or `System.Text.Json` directly).

### `IAuthStrategy`

```csharp
public interface IAuthStrategy
{
    Task<NullOperationResult> ApplyAsync(HttpRequestMessage request);
}
```

No implementation of `IAuthStrategy` ships with this module — see [Authentication via `IAuthStrategy`](#authentication-via-iauthstrategy) above.

### `ApiEndpoint`

A small helper for building endpoint URLs against a persistent base route, in one of three styles:

```csharp
public class ApiEndpoint
{
    public ApiEndpoint(string route);

    public string BuildUrl();

    public Task<OperationResult<string>> BuildQueryUrlAsync(Dictionary<string, object> queryParams);
    public OperationResult<string> BuildQueryUrl(Dictionary<string, object> queryParams);

    public OperationResult<string> BuildSlashUrl(Dictionary<string, object> queryParams);
    public OperationResult<string> BuildPositionalUrl(List<object> queryParams);
}

public enum EndpointUrlStyle { Query, Slash, Positional }
```

```csharp
var endpoint = new ApiEndpoint("users/search");

var queryUrl = endpoint.BuildQueryUrl(new() { ["name"] = "John Doe", ["age"] = 30 });
// "users/search?name=John+Doe&age=30"

var slashUrl = new ApiEndpoint("products/filter").BuildSlashUrl(new() { ["category"] = "Laptops" });
// "products/filter/category/Laptops"

var positionalUrl = new ApiEndpoint("orders/details").BuildPositionalUrl(new() { 12345, "full" });
// "orders/details/12345/full"
```

`BuildQueryUrl`/`BuildQueryUrlAsync` URL-encode via `FormUrlEncodedContent`; `BuildSlashUrl`/`BuildPositionalUrl` URL-encode each segment individually via `WebUtility.UrlEncode`. All four return a failed `OperationResult<string>` (rather than throwing) if encoding fails. `ApiEndpoint` only builds the string — pass the result straight into `ApiClient.GetAsync`/etc. as `endpointUrl`.

### `ApiRequest`

The internal static class `ApiClient` delegates to for the actual HTTP send. You don't normally call it directly — `ApiClient.GetAsync`/`PostAsync`/`PutAsync`/`DeleteAsync` are thin wrappers around `ApiRequest.GetAsync`/etc. — but it's worth knowing about since the header-handling and error-wrapping behavior `ApiClient` exposes actually lives here:

- Header handling normalizes a few well-known header names case-insensitively: `Content-Type` is set on `request.Content.Headers` (throwing if there's no content to attach it to), `Authorization` recognizes `basic`/`bearer` prefixes (falling back to treating a bare token as `Bearer`, or splitting on the first space for any other scheme), and `User-Agent` is parsed via `HttpHeaders.UserAgent.ParseAdd`. Any other header is added as-is (replacing an existing header of the same name).
- If an `IAuthStrategy` is supplied and its `ApplyAsync` fails, the request is never sent — `ApiRequest` returns a failed `ApiOperationResult<string>` with `HttpStatusCode.Unauthorized` immediately.
- `SendRequestAsync` wraps the actual `HttpClient.SendAsync` call, translating a non-2xx response into a failed `ApiOperationResult<string>` (carrying the response body and status code) and a network/protocol-level `HttpRequestException` into `HttpStatusCode.ServiceUnavailable`.

## Error handling

HTTP failures, network errors, and non-2xx responses are all captured as a failed `ApiOperationResult<string>` rather than a thrown exception — check `MethodSuccess` first, then use `StatusCode`, `ResponseBody`, and `Exception` to decide how to react:

```csharp
var result = await client.PostAsync("orders", payload.Result);

if (!result.MethodSuccess)
{
    if (result.StatusCode == HttpStatusCode.ServiceUnavailable)
    {
        // likely a network/DNS/connection failure rather than an HTTP error response
    }

    logger.LogError(result.Exception, "Request to {Url} failed: {Summary}", result.RequestUrl, result.DisplaySummary);
    return;
}
```

## Best Practices

- Use one named client per logical downstream API rather than one client per call — this keeps connection pooling, headers, and metrics scoped sensibly.
- Set an explicit `HttpTimeout_Seconds` per client rather than relying on the manager-wide default, especially for calls with different latency expectations.
- Treat GET/DELETE bodies as an advanced feature — confirm the target API actually reads them before depending on it.
- Inspect `ClientMetrics` in health checks or diagnostics endpoints rather than adding your own request counters.
