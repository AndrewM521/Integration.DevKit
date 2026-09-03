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
| `Username` / `Password` | `""` | Used for Basic auth via `SetCredentials`, or sourced from a secret store — see [Credentials](#credentials) below. |
| `MaxConcurrentRequests` | `int.MaxValue` | Negative values are coerced to `int.MaxValue`. |
| `HttpTimeout_Seconds` | `null` | `null` falls back to the manager's `Default_HttpTimeout_Seconds`. **`0` means no timeout** (mapped to `Timeout.InfiniteTimeSpan`), not "time out immediately" — negative values are coerced to `0`. |
| `DefaultHeaders` | `{}` | Applied to every request from this client. |

All named clients share a single `IHttpClientFactory`-managed connection pool internally (they're all created via the fixed factory name `"ApiClient"`); per-client `BaseUrl`, headers, and timeout are applied on top of that shared handler when each `ApiClient` is constructed.

## Making requests

```csharp
public interface IApiClient : IAsyncDisposable
{
    ApiClientSettings RuntimeSettings { get; }
    IApiClientMetrics ClientMetrics { get; }
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

### Credentials

```csharp
void SetSecretStore(ISecretStore secretStore);
NullOperationResult SetCredentials(string username, string password);
OperationResult<string> GetUsername();
OperationResult<string> GetPassword();
NullOperationResult DeleteCredential(string key);
NullOperationResult DeleteAllCredentials();
```

If a secret store is attached via `SetSecretStore`, it takes priority over the plain `Username`/`Password` values configured on `ApiClientSettings`. See [Credential Management](credential-management.md) for the store implementation.

### OAuth2 authentication

For downstream APIs that require an OAuth2 bearer token rather than Basic credentials, attach an `IAuthStrategy`:

```csharp
public interface IAuthStrategy
{
    Task<NullOperationResult> ApplyAsync(HttpRequestMessage request);
}
```

`SetAuthStrategy` is independent of `SetSecretStore` — a client can use either, both, or neither. When set, the strategy runs on every outgoing request (`GetAsync`/`PostAsync`/`PutAsync`/`DeleteAsync`, sync or async) just before it's sent, and any failure it returns short-circuits the call as a failed `ApiOperationResult<string>` (HTTP 401) rather than hitting the network.

`OAuth2ClientCredentialsAuthStrategy` is the shipped implementation, covering the client-credentials grant plus refresh-token renewal — the fit for backend, service-to-service calls (there is no authorization-code/PKCE browser flow in this module; `IApiClient` isn't used for interactive user login). It sources the `client_secret` from an [`ISecretReader`](credential-management.md#isecretreader-and-isecretstore) on every token refresh, so it composes directly with the credential management module from [Credential Management](credential-management.md) — e.g. an environment variable via `ConfigurationSecretReader`, falling back to the encrypted `FileSecretStore` via `CompositeSecretReader`:

```csharp
var secretReader = new CompositeSecretReader(new ISecretReader[]
{
    new ConfigurationSecretReader(configuration),   // e.g. env var / CI secret
    Service_CredentialMgmt.FileSecretStore,         // fall back to the encrypted store
});

var authStrategy = new OAuth2ClientCredentialsAuthStrategy(
    tokenHttpClient: httpClientFactory.CreateClient("OAuth2TokenClient"),
    tokenEndpoint: "https://idp.example.com/oauth2/token",
    clientId: "my-client-id",
    credentialContainer: "MyApi-OAuth",             // fileName passed to secretReader (and refreshTokenStore)
    secretReader: secretReader,                      // "client_secret" is read from this container
    refreshTokenStore: Service_CredentialMgmt.FileSecretStore, // optional — persists refresh_token across restarts
    scope: "orders.read orders.write");

var client = Service_RESTApiMgmt.ApiManager.GetClient("my-client");
client.SetAuthStrategy(authStrategy);

var result = await client.GetAsync("orders");
```

The access token is cached in memory and reused until 60 seconds before its `expires_in`, at which point the next request triggers a refresh (guarded so concurrent requests don't all refresh at once). If the authorization server returned a `refresh_token` and a `refreshTokenStore` was supplied, refreshes use the `refresh_token` grant instead of re-running `client_credentials`; the refreshed token is written back to the same store so it survives an application restart.

## Client metrics

```csharp
public interface IApiClientMetrics
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
public interface IApiManager
{
    ApiManagerSettings RuntimeSettings { get; set; }
    IApiClient GetClient(string clientName);
    void LogRuntimeSettings();
}
```

`GetClient` caches and reuses one `IApiClient` per name (case-insensitive) — calling it twice with the same name returns the same instance. If `clientName` isn't found in the configured `Clients` dictionary, it logs a warning and hands back a client built from default `ApiClientSettings` rather than throwing, so a typo in a client name fails silently at the HTTP layer (wrong base URL) rather than at `GetClient` — double-check the name against your configuration if requests are going to the wrong host.

## API Reference

### `Service_RESTApiMgmt` (static)

```csharp
public static IServiceCollection AddRESTApiMgmt(this IServiceCollection services, IConfiguration configuration);
public static void Initialize(IServiceProvider sp);
public static IApiManager ApiManager { get; }   // throws InvalidOperationException before Initialize
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

### `IAuthStrategy` / `OAuth2ClientCredentialsAuthStrategy`

```csharp
public interface IAuthStrategy
{
    Task<NullOperationResult> ApplyAsync(HttpRequestMessage request);
}

public class OAuth2ClientCredentialsAuthStrategy : IAuthStrategy
{
    public OAuth2ClientCredentialsAuthStrategy(
        HttpClient tokenHttpClient,
        string tokenEndpoint,
        string clientId,
        string credentialContainer,
        ISecretReader secretReader,
        ISecretStore? refreshTokenStore = null,
        string? scope = null);

    public Task<NullOperationResult> ApplyAsync(HttpRequestMessage request);
}
```

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
