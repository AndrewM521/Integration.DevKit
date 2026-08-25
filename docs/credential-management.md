# Credential Management

`Integration.DevKit.CredentialMgmt` provides `ISecretStore`, a small key/value secret-storage abstraction, plus one concrete implementation — `FileSecretStore` — that persists secrets to disk, encrypted at rest using ASP.NET Core's Data Protection stack. It's the store that [REST API Management](rest-api.md#credentials) and [SQL Management](sql-management.md#credentials) can optionally plug into via `SetSecretStore`.

## Requirements

- .NET 8
- `Microsoft.AspNetCore.DataProtection`

## Installation

```bash
dotnet add reference src/Integration.DevKit.CredentialMgmt/Integration.DevKit.CredentialMgmt.csproj
```

Or from NuGet: [Integration.DevKit.CredentialMgmt](https://www.nuget.org/packages/Integration.DevKit.CredentialMgmt)

## Getting started

```csharp
using Integration.DevKit.CredentialMgmt;

services.AddFileSecretStore(
    applicationName: "MyApp",
    secretsFolder: @"C:\MyApp\Secrets",
    keysFolder: @"C:\MyApp\Keys");

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

This module has **no config-section binding** — `AddFileSecretStore` takes its parameters directly as arguments rather than reading an `IConfiguration` section, and there's no ordering dependency on any other DevKit module (unlike, say, `ThreadSafeItems` needing `ThreadLocks`).

## `ISecretStore`

```csharp
namespace Integration.DevKit.CredentialMgmt.Contracts;

public interface ISecretStore
{
    string StoreName { get; }
    NullOperationResult SetKey(string fileName, string key, string value);
    OperationResult<string> GetKey(string fileName, string key);
    NullOperationResult DeleteKey(string fileName, string key);
    NullOperationResult DeleteSecret(string fileName);
}
```

Think of `fileName` as a **container name** — a logical grouping of related secrets (e.g. `"Api"`, `"Database"`) — and `key` as the individual secret's name within that container. The interface contract requires implementations to encrypt data at rest and restrict access to the underlying storage; `FileSecretStore` is the one shipped implementation that satisfies this.

## `FileSecretStore`

```csharp
namespace Integration.DevKit.CredentialMgmt;

public class FileSecretStore : SecretStoreBase
{
    public FileSecretStore(IDataProtectionProvider provider, string applicationName, string secretsDir);

    public override NullOperationResult SetKey(string fileName, string key, string value);
    public override OperationResult<string> GetKey(string fileName, string key);
    public override NullOperationResult DeleteKey(string fileName, string key);
    public override NullOperationResult DeleteSecret(string fileName);
}
```

You don't normally construct this directly — `AddFileSecretStore` registers and wires it up for you. If you do need to, note that its base class `SecretStoreBase` has a `protected` constructor and `protected` `Encrypt`/`Decrypt` helpers, so `FileSecretStore` is the only way to use this module today (there is currently no other concrete `ISecretStore` implementation shipped).

### How secrets are stored

- Each container (`fileName`) is written to its own file: `secretsDir/FileSecretStore_{fileName}.secret` (invalid filename characters in `fileName` are replaced with `_`).
- All keys within a container are held as a single JSON dictionary, serialized, and then the **entire JSON blob is encrypted as one unit** using an ASP.NET Core Data Protection `IDataProtector` — only the resulting ciphertext is ever written to disk.
- Writes are atomic: content is written to a temporary file first, then moved into place, so a crash mid-write can't leave a corrupted secrets file.
- `GetKey` decrypts and re-parses the file on every call — there's no in-memory cache, so reads always reflect the latest state on disk (and updating a value is always visible to subsequent reads without a restart).
- `DeleteKey` and `DeleteSecret` are idempotent — deleting a key or container that doesn't exist returns success rather than failing.
- Values are trimmed of leading/trailing whitespace both when stored and when read back.

### Where the encryption keys live

`AddFileSecretStore` configures ASP.NET Core Data Protection to persist its key ring to `keysFolder` as XML files (`PersistKeysToFileSystem`). This is what actually encrypts/decrypts your secrets — protect `keysFolder` with the same care as the secrets themselves (file-system permissions, backup/rotation policy), since anyone with read access to the key ring plus the `.secret` files can decrypt them. Data Protection does not encrypt the key ring itself unless you additionally configure a key-ring protector (e.g. Windows DPAPI or a certificate) — this module doesn't configure one, so treat `keysFolder` as sensitive, access-controlled storage.

> **Known quirk:** internally, the Data Protection "purpose" string used to create the protector is hardcoded to the literal `"FileSecretStore"` rather than your `applicationName` — `applicationName` only affects the Data Protection application discriminator (`SetApplicationName`), not the protector purpose itself. This doesn't weaken security, but it means the purpose string is the same across every application using this module rather than being application-specific.

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
public static IServiceCollection AddFileSecretStore(this IServiceCollection services, string applicationName, string secretsFolder, string keysFolder);
public static void InitializeFileSecretStore(IServiceProvider sp);
public static FileSecretStore FileSecretStore { get; }   // throws InvalidOperationException before Initialize
```

## Error Handling

Every `ISecretStore` method returns `NullOperationResult`/`OperationResult<string>` rather than throwing — check `MethodSuccess` before trusting `Result`. `GetKey` on a missing key returns a failed result wrapping a `KeyNotFoundException` rather than throwing directly.

## Best Practices

- Restrict file-system access to both `secretsFolder` and `keysFolder` to the accounts/processes that need them — losing either one alone is recoverable, losing both together is not.
- Use one container (`fileName`) per logical group of secrets (e.g. per integration or per environment) rather than one giant container for everything.
- Back up `keysFolder` alongside your secrets — if the Data Protection key ring is lost, existing `.secret` files become permanently undecryptable.
- Prefer this module (or a managed secret store) over plain values in `appsettings.json`, and reserve [`AesConfigProtector`](core.md#configuration-protection) for lower-stakes obfuscation only.
