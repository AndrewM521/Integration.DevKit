# Credential Management

`Integration.DevKit.CredentialMgmt` doesn't try to replace every place a secret might live — env vars, ASP.NET Core User Secrets, a cloud vault, or its own encrypted file store. Instead it standardizes the *contract* other DevKit modules talk to (`ISecretReader`/`ISecretStore`), ships one concrete read/write implementation (`FileSecretStore`, encrypted at rest via ASP.NET Core's Data Protection stack), and gives you composition helpers (`ConfigurationSecretReader`, `CompositeSecretReader`, `ImportFrom`) so you can layer in whatever other sources a project needs without writing provider-specific glue. It's the store that [REST API Management](rest-api.md#credentials) and [SQL Management](sql-management.md#credentials) can optionally plug into via `SetSecretStore`.

## Requirements

- .NET 8
- `Microsoft.AspNetCore.DataProtection`

## Installation

```bash
dotnet add reference src/Integration.DevKit.CredentialMgmt/Integration.DevKit.CredentialMgmt.csproj
```

Or from NuGet: [Integration.DevKit.CredentialMgmt](https://www.nuget.org/packages/Integration.DevKit.CredentialMgmt)

## Getting started

Registration is config-driven — `AddCredentialMgmt` reads the `Integration.DevKit:CredentialManagement` section and selects a backend by its `Provider` name (the built-in `"File"` provider, or a custom one — see [Config-driven registration](#config-driven-registration) below):

```csharp
using Integration.DevKit.CredentialMgmt;
using Microsoft.Extensions.Configuration;

var configuration = new ConfigurationBuilder()
    .AddInMemoryCollection(new Dictionary<string, string?>
    {
        ["Integration.DevKit:CredentialManagement:Provider"] = "File",
        ["Integration.DevKit:CredentialManagement:Options:ApplicationName"] = "MyApp",
        ["Integration.DevKit:CredentialManagement:Options:SecretsFolder"] = @"C:\MyApp\Secrets",
        ["Integration.DevKit:CredentialManagement:Options:KeysFolder"] = @"C:\MyApp\Keys",
    })
    .Build();

services.AddCredentialMgmt(configuration);

// ... build the host ...
Service_CredentialMgmt.InitializeFileSecretStore(app.Services);

var store = Service_CredentialMgmt.FileSecretStore;

store.SetKey("Api", "ApiKey", "super-secret-value");
var apiKey = store.GetKey("Api", "ApiKey");

if (apiKey.MethodSuccess)
{
    Console.WriteLine(apiKey.Result);
}
```

The `"File"` provider's `Options` are typically loaded from `appsettings.json` rather than built in-memory like above — see the full JSON shape in [Config-driven registration](#config-driven-registration). Registering the `"File"` provider also registers `ThreadLocks` for you automatically (`FileSecretStore` needs an `IThreadLockManager` to serialize concurrent access to the same container's file) — you don't need to call `AddThreadLocks()` yourself unless you're also using that module directly.

## `ISecretReader` and `ISecretStore`

```csharp
namespace Integration.DevKit.CredentialMgmt.Contracts;

public interface ISecretReader
{
    string StoreName { get; }
    OperationResult<string> GetKey(string fileName, string key);
}

public interface ISecretStore : ISecretReader
{
    NullOperationResult SetKey(string fileName, string key, string value);
    NullOperationResult DeleteKey(string fileName, string key);
    NullOperationResult DeleteSecret(string fileName);
}
```

`ISecretReader` is the minimal, read-only contract — it's what env vars, `IConfiguration`, and read-only cloud-vault views can implement in a handful of lines, without taking on write support they can't (or shouldn't) provide. `ISecretStore` extends it with the write operations, for backends — like `FileSecretStore` — that actually own their storage.

Think of `fileName` as a **container name** — a logical grouping of related secrets (e.g. `"Api"`, `"Database"`) — and `key` as the individual secret's name within that container. `ISecretStore` implementations must encrypt data at rest and restrict access to the underlying storage; `FileSecretStore` is the one shipped implementation that satisfies this. `ISecretReader` carries no such guarantee itself — a config-backed reader is only as safe as the configuration source behind it (e.g. User Secrets are not encrypted at rest).

## `FileSecretStore`

```csharp
namespace Integration.DevKit.CredentialMgmt;

public class FileSecretStore : SecretStoreBase
{
    public FileSecretStore(
        IDataProtectionProvider provider,
        string applicationName,
        string secretsDir,
        IThreadLockManager threadLockManager,
        ILoggerFactory? loggerFactory = null,
        bool enableLogging = true);

    public bool EnableLogging { get; set; }   // mutate at runtime to silence/resume this store's logging

    public override NullOperationResult SetKey(string fileName, string key, string value);
    public override OperationResult<string> GetKey(string fileName, string key);
    public override NullOperationResult DeleteKey(string fileName, string key);
    public override NullOperationResult DeleteSecret(string fileName);
}
```

You don't normally construct this directly — `AddCredentialMgmt` registers and wires it up for you. If you do need to, note that its base class `SecretStoreBase` has a `protected` constructor and `protected` `Encrypt`/`Decrypt` helpers, so `FileSecretStore` is the only way to use this module today (there is currently no other concrete `ISecretStore` implementation shipped).

### How secrets are stored

- Each container (`fileName`) is written to its own file: `secretsDir/FileSecretStore_{fileName}.secret` (invalid filename characters in `fileName` are replaced with `_`).
- All keys within a container are held as a single JSON dictionary, serialized, and then the **entire JSON blob is encrypted as one unit** using an ASP.NET Core Data Protection `IDataProtector` — only the resulting ciphertext is ever written to disk.
- Writes are atomic: content is written to a temporary file first, then moved into place, so a crash mid-write can't leave a corrupted secrets file.
- `GetKey` decrypts and re-parses the file on every call — there's no in-memory cache, so reads always reflect the latest state on disk (and updating a value is always visible to subsequent reads without a restart).
- `DeleteKey` and `DeleteSecret` are idempotent — deleting a key or container that doesn't exist returns success rather than failing.
- Values are trimmed of leading/trailing whitespace both when stored and when read back.

### Where the encryption keys live

`AddCredentialMgmt`'s `"File"` provider configures ASP.NET Core Data Protection to persist its key ring to `keysFolder` as XML files (`PersistKeysToFileSystem`). This is what actually encrypts/decrypts your secrets — protect `keysFolder` with the same care as the secrets themselves (file-system permissions, backup/rotation policy), since anyone with read access to the key ring plus the `.secret` files can decrypt them. Data Protection does not encrypt the key ring itself unless you additionally configure a key-ring protector (e.g. Windows DPAPI or a certificate) — this module doesn't configure one, so treat `keysFolder` as sensitive, access-controlled storage.

> **Known quirk:** internally, the Data Protection "purpose" string used to create the protector is hardcoded to the literal `"FileSecretStore"` rather than your `applicationName` — `applicationName` only affects the Data Protection application discriminator (`SetApplicationName`), not the protector purpose itself. This doesn't weaken security, but it means the purpose string is the same across every application using this module rather than being application-specific.

## Composing multiple sources

### `ConfigurationSecretReader`

A thin, read-only wrapper over `IConfiguration`. Because ASP.NET Core User Secrets, environment variables, and command-line arguments are all just `IConfiguration` providers, this one class makes all of them valid secret sources — there's no separate "User Secrets adapter" to write.

```csharp
var reader = new ConfigurationSecretReader(configuration);
var apiKey = reader.GetKey("Api", "ApiKey"); // looks up configuration["Api:ApiKey"]
```

`GetKey` reads `{fileName}:{key}` as a hierarchical configuration path. This matches both nested JSON (`"Api": { "ApiKey": "..." }`) and an environment variable named `Api__ApiKey`, since `IConfiguration` normalizes double underscores to colons for its environment variable provider.

### `CompositeSecretReader`

Tries a prioritized list of `ISecretReader`s in order and returns the first successful result — the same layering model `IConfiguration` itself uses.

```csharp
var reader = new CompositeSecretReader(new ISecretReader[]
{
    new ConfigurationSecretReader(configuration), // e.g. env var / CI secret / User Secrets
    Service_CredentialMgmt.FileSecretStore,       // fall back to the encrypted store
});

var apiKey = reader.GetKey("Api", "ApiKey");
```

The order is entirely the consuming project's decision — DevKit only supplies the composition logic, not an opinion about which source should win.

### `ImportFrom`

Seeds an `ISecretStore` from any `ISecretReader`, in one call — the same code path regardless of where the plaintext originally came from:

```csharp
Service_CredentialMgmt.FileSecretStore.ImportFrom(
    new ConfigurationSecretReader(configuration), "Api", "ApiKey");
```

This is the recommended way to bootstrap the encrypted `FileSecretStore` from User Secrets (locally) or environment variables/CI secrets (in automated environments) without hand-rolling a one-off import script per source — the plaintext value only ever exists in memory for the duration of this call.

## Config-driven registration

`AddCredentialMgmt(configuration)` selects and registers a backend from an `Integration.DevKit:CredentialManagement` configuration section, matching the binding convention used by [`AddSQLMgmt`](sql-management.md) and [`AddRESTApiMgmt`](rest-api.md) — this is the same call shown in [Getting started](#getting-started), typically loaded from `appsettings.json` instead of built in-memory:

```json
{
  "Integration.DevKit": {
    "CredentialManagement": {
      "Provider": "File",
      "EnableLogging": true,
      "Options": {
        "ApplicationName": "MyApp",
        "SecretsFolder": "C:\\MyApp\\Secrets",
        "KeysFolder": "C:\\MyApp\\Keys"
      }
    }
  }
}
```

`EnableLogging` (default `true`) is checked fresh on every log call rather than only at startup — flip it at runtime via `Service_CredentialMgmt.FileSecretStore.EnableLogging = false;` to silence this module's logging without detaching the `ILoggerFactory` you registered for the rest of the app.

`Provider` currently only supports `"File"` out of the box, delegating to the built-in `"File"` provider registration under the hood using the bound `Options` values (`ApplicationName`, `SecretsFolder`, `KeysFolder`) — an unsupported value throws `NotSupportedException` at registration time. Register a custom provider (e.g. a cloud secrets vault) via `Service_CredentialMgmt.RegisterProvider` before calling `AddCredentialMgmt`, and it becomes selectable the same way by name.

## Wiring into `ApiClient` / `SQLClient`

Both [`IApiClient`](rest-api.md) and [`ISQLClient`](sql-management.md) accept a secret store via `SetSecretStore(ISecretStore secretStore)`, after which they'll read/write their own credentials through it instead of the plain-text values in configuration:

```csharp
var client = Service_RESTApiMgmt.ApiManager.GetClient("my-client");
client.SetSecretStore(Service_CredentialMgmt.FileSecretStore);
client.SetCredentials("api-user", "api-password");

// later, from anywhere holding a reference to the same client:
var username = client.GetUsername();
```

Each client stores its credentials under its own container name, derived internally (both `ApiClient` and `SQLClient` currently use a container name in the form `ApiClient({ClientName})` — a copy-paste artifact on the SQL side that's harmless but worth knowing if you're inspecting `.secret` files directly on disk). This integration is entirely opt-in: neither client requires a secret store to function, and calling `SetCredentials`/`SetSecretStoreCredentials` before `SetSecretStore` throws (since there's nowhere to store the value yet).

## API Reference

### `Service_CredentialMgmt` (static)

```csharp
public static IServiceCollection AddCredentialMgmt(this IServiceCollection services, IConfiguration configuration);
public static void RegisterProvider(string providerName, Action<IServiceCollection, Dictionary<string, object>, bool, IConfiguration> register);
public static void InitializeFileSecretStore(IServiceProvider sp);
public static FileSecretStore FileSecretStore { get; }   // throws InvalidOperationException before Initialize
```

### `ConfigurationSecretReader` / `CompositeSecretReader`

```csharp
public class ConfigurationSecretReader : ISecretReader
{
    public ConfigurationSecretReader(IConfiguration configuration, string storeName = "ConfigurationSecretReader");
    public OperationResult<string> GetKey(string fileName, string key);
}

public class CompositeSecretReader : ISecretReader
{
    public CompositeSecretReader(IReadOnlyList<ISecretReader> readers, string storeName = "CompositeSecretReader");
    public OperationResult<string> GetKey(string fileName, string key);
}
```

### `SecretStoreExtensions`

```csharp
public static NullOperationResult ImportFrom(this ISecretStore target, ISecretReader source, string fileName, string key);
```

## Error Handling

Every `ISecretReader`/`ISecretStore` method returns `OperationResult<string>`/`NullOperationResult` rather than throwing — check `MethodSuccess` before trusting `Result`. `GetKey` on a missing key returns a failed result wrapping a `KeyNotFoundException` rather than throwing directly; `CompositeSecretReader` does the same once every configured reader has failed.

## Best Practices

- Restrict file-system access to both `secretsFolder` and `keysFolder` to the accounts/processes that need them — losing either one alone is recoverable, losing both together is not.
- Use one container (`fileName`) per logical group of secrets (e.g. per integration or per environment) rather than one giant container for everything.
- Back up `keysFolder` alongside your secrets — if the Data Protection key ring is lost, existing `.secret` files become permanently undecryptable.
- Prefer this module (or a managed secret store) over plain values in `appsettings.json`, and reserve [`AesConfigProtector`](core.md#configuration-protection) for lower-stakes obfuscation only.
