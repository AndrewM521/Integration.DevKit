# Core

`Integration.DevKit.Core` is the foundation package that every other Integration.DevKit module depends on. It has no dependency on any other DevKit package. It provides:

- A consistent result-object pattern (`OperationResult<T>` and its variants) used across the entire SDK instead of exceptions for expected failure paths.
- Configuration protection: encrypting and decrypting values inside `appsettings.json`.
- File, directory, dictionary, and JSON utilities used internally by the other modules (and safe to use directly).
- `OnDemandHost`, a lightweight service-container/host for applications that are not already built on `Microsoft.Extensions.Hosting`.

## Requirements

- .NET 8
- `Microsoft.Extensions.Configuration` (for the configuration-protection pipeline)
- `Microsoft.Extensions.Hosting.Abstractions` (for `OnDemandHost` and `IHostApplicationLifetime`)

## Installation

```bash
dotnet add reference src/Integration.DevKit.Core/Integration.DevKit.Core.csproj
```

Every other DevKit project already references Core transitively, so you rarely need to add it explicitly unless you're consuming Core's utilities on their own.

---

## Result types

Nearly every public method across the SDK returns a result object instead of throwing. All variants live in the `Integration.DevKit.Core` namespace and implement `IOperationResult<T>`:

| Type | Use when | `Result` may be `null`? |
| --- | --- | --- |
| `OperationResult<T>` | The operation produces a real value on success. | No — assigning a `null` result via `SetMethodSuccess` automatically converts the object to a failure. |
| `NullableOperationResult<T>` | The operation may legitimately succeed with no value. | Yes. |
| `NullOperationResult` | The operation has no payload at all — success/failure only. | N/A (`Result` is always `object?`). |
| `ApiOperationResult<T>` | An HTTP call — adds `RequestUrl`, `StatusCode`, `ResponseBody`, `DisplaySummary` on top of `NullableOperationResult<T>`. | Yes. |

Every variant exposes the same three read-only members plus a fluent setter pair:

```csharp
public bool MethodSuccess { get; }
public T Result { get; }
public Exception Exception { get; }   // never null — defaults to new Exception("No Error") when unset

// OperationResult<T>
result.SetMethodSuccess(value);
result.SetMethodFailure(ex, defaultValueOnFailure);

// NullableOperationResult<T> / NullOperationResult / ApiOperationResult<T> follow the same pattern,
// with SetMethodSuccess accepting null and NullOperationResult.SetMethodSuccess() taking no argument.
```

Calling code should check `MethodSuccess` before trusting `Result`:

```csharp
var written = FileUtils.WriteToFile(path, content);
if (!written.MethodSuccess)
{
    logger.LogError(written.Exception, "Failed to write {Path}", path);
    return;
}
```

`ApiOperationResult<T>` additionally carries HTTP context and is covered in detail in [REST API Management](rest-api.md).

---

## Configuration protection

Configuration protection lets you keep secrets in `appsettings.json` in an encrypted-looking form and transparently decrypt them into the running `IConfiguration` tree. It is **not** a general-purpose encryption library — it's specifically shaped around round-tripping values through JSON configuration files.

### `IConfigProtector`

```csharp
namespace Integration.DevKit.Core;

public interface IConfigProtector
{
    string Name { get; }
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}
```

`Name` is the identifier written into the encrypted value's prefix (see `ConfigProtectorContract` below) so the correct protector can be selected again at decrypt time. Two implementations ship out of the box:

#### `Base64ConfigProtector`

```csharp
namespace Integration.DevKit.Core;

public sealed class Base64ConfigProtector : IConfigProtector   // Name => "BASE64"
```

Encodes/decodes with Base64. **This provides no confidentiality** — anyone can decode a Base64 string by hand. Use it only to obscure values from casual viewing (e.g. keeping a value out of plain sight in a diff or screen share), never to protect an actual secret.

#### `AesConfigProtector`

```csharp
namespace Integration.DevKit.Core.Configuration;

public sealed class AesConfigProtector : IConfigProtector   // Name => "AES256"

public AesConfigProtector(string encryptionKey, string iv)
public string Encrypt(string plainText)
public string Decrypt(string cipherText)
```

> **Security note — read before using this in production.** The key and IV are derived by right-padding your input strings and truncating to 32 bytes (key) and 16 bytes (IV) — they are **not** run through a key-derivation function, and the same fixed IV is reused for every value encrypted by a given instance. AES is used in CBC mode (the .NET default), and reusing an IV across multiple encryptions of related plaintext is a known weakness (it can leak whether two encrypted values share a common prefix). Treat `AesConfigProtector` as "better than Base64, not as strong as a proper secrets manager": prefer a real key vault or managed secret store for anything sensitive in a production system, and if you do use it, supply a high-entropy, random `encryptionKey`/`iv` pair generated once and stored outside source control.

Both throw `ArgumentNullException` if a required constructor argument is null/whitespace (`AesConfigProtector` only). `Encrypt`/`Decrypt` pass through unchanged when given a null or empty string.

### `ConfigProtectorContract`

Describes how an encrypted value is tagged so the pipeline can find and route it:

```csharp
public sealed class ConfigProtectorContract(char delimiter = ':')
{
    public char Delimiter { get; }
    public string Signature { get; init; } = "ENC";   // must not contain the delimiter
    public string Version { get; init; } = "v1";      // must not contain the delimiter

    public string BuildPrefix(IConfigProtector protector);
    // => $"{Signature}{Version}{Delimiter}{protector.Name}{Delimiter}"
    // e.g. "ENCv1:AES256:"
}
```

A protected value looks like `ENCv1:AES256:<base64 ciphertext>` — the prefix tells the decryption pipeline which registered `IConfigProtector` to hand the payload to.

### Encrypting values into a JSON file

`EncryptionOptions` + `JsonEncryptor`, wired through the extension method `ConfigProtectorExtensions.EncryptJsonOnBuild`, walk a JSON file and replace targeted values with their encrypted form:

```csharp
public sealed class EncryptionOptions
{
    public string FileName { get; set; } = "appsettings.json";
    public string EncryptSigniture { get; set; } = "ENC";   // note: this is the actual property name in source
    public bool ThrowOnMissingFile { get; set; } = true;
    public bool EncryptStrings { get; set; } = true;
    public bool EncryptIntegers { get; set; } = false;
    public bool EncryptBooleans { get; set; } = false;
    public bool EncryptDecimals { get; set; } = false;

    public void Encrypt(string jsonPath, IConfigProtector? protector = null);   // protector defaults to Base64ConfigProtector
}
```

```csharp
using Integration.DevKit.Core;
using Integration.DevKit.Core.Configuration;
using Microsoft.Extensions.Configuration;

var contract = new ConfigProtectorContract();
var aes = new AesConfigProtector(myKey, myIv);

new ConfigurationBuilder()
    .EncryptJsonOnBuild(contract, options =>
    {
        options.FileName = "appsettings.json";
        options.Encrypt("Integration:DevKit:SQLManagement:Clients:Default:ConnectionString", aes);
    });
```

`EncryptJsonOnBuild` rewrites the file in place; it does not itself register the value into the returned `IConfigurationBuilder`. Values already carrying the configured `Signature` prefix are left untouched (idempotent — running it twice will not double-encrypt).

### Decrypting values when building configuration

`ConfigProtectorExtensions.DecryptJsonOnBuild` adds a configuration source that decrypts every matching value as the configuration is built:

```csharp
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .DecryptJsonOnBuild(contract, new List<IConfigProtector> { aes, new Base64ConfigProtector() })
    .Build();
```

Internally this builds an intermediate `IConfiguration` from whatever sources were registered so far, then adds a `DecryptionConfigSource`/`DecryptionConfigProvider` pair that walks every leaf value: if it starts with `contract.Signature`, it's split on `contract.Delimiter` into `{signature+version}{delimiter}{providerName}{delimiter}{payload}`, and the matching `IConfigProtector` (looked up by `Name` in the list you pass in) decrypts the payload. Values that don't match the signature pass through unchanged. A malformed encrypted-looking value throws `FormatException`; an unrecognized provider name throws `KeyNotFoundException`.

---

## On-demand hosting

`Integration.DevKit.Core.OnDemand` provides a minimal static host for applications that want DevKit's DI-based modules (logging, task management, etc.) without building a full `Microsoft.Extensions.Hosting` generic host.

```csharp
public static class OnDemandHost
{
    public static IServiceProvider Services { get; }   // throws InvalidOperationException until Start/StartAsync has run

    public static void ConfigureServices(Action<IServiceCollection> configureAction);
    public static Task StartAsync(IConfiguration? configuration = null, CancellationToken cancellationToken = default);
    public static void Start(IConfiguration? configuration = null);   // blocks on StartAsync
    public static Task StopAsync();
    public static void Stop();                                        // blocks on StopAsync
}
```

`ConfigureServices` registers your services into a shared, static `IServiceCollection` (`OnDemand_Registry.ServiceCollection`). `Start`/`StartAsync` builds the provider, registers a shutdown hook on `AppDomain.CurrentDomain.ProcessExit`, and starts every registered `IHostedService`. `Stop`/`StopAsync` signals the internal `OnDemand_Lifetime` (a minimal `IHostApplicationLifetime` implementation) and stops those hosted services.

```csharp
OnDemandHost.ConfigureServices(services =>
{
    services.AddCustomLogging(configuration);
    services.AddTaskMgmt(configuration);
});

await OnDemandHost.StartAsync(configuration);

// later
await OnDemandHost.StopAsync();
```

Most applications that already use `Host.CreateDefaultBuilder` do **not** need `OnDemandHost` — it exists for embedding DevKit modules into apps that don't otherwise have a hosting/DI pipeline (e.g. a console tool, a plugin, or legacy code).

---

## File and directory utilities

`FileUtils` and `DirectoryUtils` (both `Integration.DevKit.Core`) wrap `System.IO` with the SDK's result-object pattern and consistent path validation. Every operation validates its path first via `IsStringValidFilePath` / `IsStringValidDirectoryPath` and fails fast with a descriptive exception rather than letting a raw `IOException` surface.

Representative members (each has a synchronous and, where noted, asynchronous form):

```csharp
// FileUtils
NullOperationResult WriteToFile(string path, string content, bool append = false, bool allowNoFileExtension = false, Encoding? encoding = null);
NullOperationResult WriteToFile(string path, string[] content, ...);
NullOperationResult WriteBytesToFile(string path, byte[] content, ...);
OperationResult<string> ReadFileText(string path, bool allowNoFileExtension = false);
OperationResult<string[]> ReadFileLines(string path, bool allowNoFileExtension = false);
OperationResult<byte[]> ReadFileBytes(string path, bool allowNoFileExtension = false);
Task<NullOperationResult> WriteToFileAsync(...);       // async equivalents of the above
Task<OperationResult<string>> ReadFileTextAsync(...);
NullOperationResult CreateFile(string path, bool allowNoFileExtension = false);
NullOperationResult DeleteFile(string path, bool allowNoFileExtension = false);
NullOperationResult DeleteFiles(string path, string searchPattern, bool allowNoFileExtension = false);
NullOperationResult CopyFile(string sourcePath, string destinationPath, bool overwrite = false, ...);
NullOperationResult MoveFile(string sourcePath, string destinationPath, bool overwrite = false, ...);
OperationResult<string> GetExtension(string path, bool allowNoFileExtension = false);
OperationResult<bool> IsPathValidExtension(string filePath, string validExtension);
OperationResult<bool> IsStringValidFilePath(string path, bool allowNoFileExtension = false);
bool DoesFileExist(string path);

// DirectoryUtils
NullOperationResult CreateDirectory(string path);
NullOperationResult DeleteDirectory(string path, bool recursive = false);
OperationResult<string[]> GetFiles(string path, SearchOption searchOption = SearchOption.TopDirectoryOnly, string searchPattern = "*");
OperationResult<bool> IsStringValidDirectoryPath(string path);
OperationResult<string> GetSafeDirectoryName(string directoryName, char replacement = '_');
bool DoesDirectoryExist(string path);
```

By default, a path must end in a recognizable extension (e.g. `.txt`) to be considered a valid *file* path — pass `allowNoFileExtension: true` for extension-less files such as `Dockerfile` or `LICENSE`. Writes create any missing parent directory automatically and append a trailing newline if the content doesn't already end with one.

For file access that must be safe across concurrent threads/tasks, use [`ThreadSafeFileIO`](thread-locks.md#thread-safe-file-io) instead of calling `FileUtils` directly from multiple callers.

---

## JSON utilities

`JsonUtils` (`Integration.DevKit.Core`) provides serialization plus a path-based extraction API for pulling specific values or sub-trees out of loosely-structured JSON without modeling the whole payload as a C# type — useful when consuming a third-party API response where you only need a few fields.

```csharp
public static class JsonUtils
{
    NullableOperationResult<object?> DeserializeJsonToObject(string json);
    OperationResult<string> SerializeObjectToJson(object obj);   // indented output

    OperationResult<Dictionary<string, object>> GetDictionary(string json, string keyPath, JsonExtractionLayout layout = JsonExtractionLayout.ExtractParent, bool removeNulls = true);
    OperationResult<Dictionary<string, object>> GetDictionary(string json, List<string>? keyPaths = null, ...);
    OperationResult<List<T>> GetList<T>(string json, string keyPath, ...);
    OperationResult<List<Dictionary<string, object>>> GetDictionaryList(string json, string keyPath, ...);
    OperationResult<T> ParseAndFilterJson<T>(string rawJson, List<string>? keys = null, JsonExtractionLayout layout = JsonExtractionLayout.ExtractParent, bool removeNulls = true);

    object? ConvertJsonElementToNativeObject(JsonElement element);
}

public enum JsonExtractionLayout
{
    ExtractParent,  // flat structure starting at the closest shared parent container
    PreserveRoot    // reconstructs the full nested tree from the document root
}
```

Key paths use dot notation (`"data.activities.name"`), including numeric indices for array elements (`"data.activities.0.id"`). On any parse failure, the failed `OperationResult<T>` still returns a usable empty instance (`Dictionary<string, object>` or `List<T>`) as its default, so callers can safely iterate the result even after a failure without an extra null check.

```csharp
var result = JsonUtils.GetDictionary(responseJson, "data.user");
if (result.MethodSuccess)
{
    var name = DictionaryUtils.GetValue<string>(result.Result, "profile.displayName", "Unknown");
}
```

## Dictionary utilities

`DictionaryUtils` (`Integration.DevKit.Core`) navigates the loosely-typed `Dictionary<string, object>` trees produced by `JsonUtils`:

```csharp
public static class DictionaryUtils
{
    T GetValue<T>(Dictionary<string, object> dictionary, string keyPath, T defaultValue = default!);
    Dictionary<string, object> GetDictionary(Dictionary<string, object> dictionary, string keyPath);
    List<T> GetList<T>(Dictionary<string, object> dictionary, string keyPath);
    List<Dictionary<string, object>> GetDictionaryList(Dictionary<string, object> dictionary, string keyPath);
    OperationResult<Dictionary<string, object>> GetFirstDictionary(List<Dictionary<string, object>> dictionaries);
    OperationResult<Dictionary<string, object>> FlattenListByKey(Dictionary<string, object> source, string searchKey);
}
```

`GetValue<T>` walks a dot-notation `keyPath`, converting the located value to `T` (including element-by-element conversion for `List<T>`), and returns `defaultValue` if any segment is missing, the value is null, or conversion fails — it never throws.

## Miscellaneous helpers

- **`GCManager.CallGC_Collect(string? description = null)`** — throttled, thread-safe forced garbage collection. Only actually collects when the managed heap has grown by more than 20 MB or more than 20 minutes have elapsed since the last forced collection; otherwise it's a no-op. Intended for long-running services with bursty allocation patterns, not for general use in request paths.
- **`MiscUtils.ConvertUnixToCentralTime(long unixSeconds)`** — converts a Unix timestamp to local time in the US Central time zone, resolving the correct time zone ID for Windows (`"Central Standard Time"`) vs. Linux/containers (`"America/Chicago"`) automatically.

## Best Practices

- Always check `MethodSuccess` before reading `Result` — don't rely on `Result` being a sensible default on failure unless you've confirmed that particular method guarantees one.
- Don't use `AesConfigProtector` as a substitute for a real secrets manager in production — see the security note above.
- Prefer `ThreadSafeFileIO` over direct `FileUtils` calls whenever the same file might be accessed concurrently from more than one place.
- Keep encryption keys, IVs, and connection strings out of source control regardless of which `IConfigProtector` you use.
