# Configuration protection

Part of [Integration.DevKit.Core](README.md).

Configuration protection lets you keep secrets in `appsettings.json` in an encrypted form and transparently decrypt them into the running `IConfiguration` tree. It is **not** a general-purpose encryption library. It's shaped specifically around round-tripping values through JSON configuration files.

## Included types

| Type | Namespace | What it does |
| --- | --- | --- |
| `IConfigProtector` | `Integration.DevKit.Core` | Interface for an encrypt/decrypt implementation. |
| `Base64ConfigProtector` | `Integration.DevKit.Core` | Base64 encoding. Obscures values; does **not** protect them. |
| `AesConfigProtector` | `Integration.DevKit.Core.Configuration` | AES-256 encryption with a supplied key and IV. |
| `ConfigProtectorContract` | `Integration.DevKit.Core` | Defines the prefix that tags a value as encrypted (e.g. `ENCv1:AES256:`). |
| `EncryptionOptions` | `Integration.DevKit.Core` | Which file and which JSON paths to encrypt. |
| `ConfigProtectorExtensions` | `Integration.DevKit.Core` | `EncryptJsonOnBuild` and `DecryptJsonOnBuild` extension methods on `IConfigurationBuilder`. |
| `JsonEncryptor`, `JsonDecryptor`, `DecryptionConfigSource`, `DecryptionConfigProvider` | `Integration.DevKit.Core` | Internals of the pipeline. You don't normally use them directly. |

## How it works

A protected value looks like `ENCv1:AES256:<base64 ciphertext>`. The prefix is built by `ConfigProtectorContract` as `{Signature}{Version}{Delimiter}{protector.Name}{Delimiter}`, and tells the decryption pipeline which registered `IConfigProtector` should decrypt the payload.

## How to

### Choose a protector

Two implementations ship out of the box:

- **`Base64ConfigProtector`** (`Name` = `"BASE64"`): encodes and decodes with Base64. **This provides no confidentiality.** Anyone can decode a Base64 string by hand. Use it only to keep a value out of plain sight (in a diff or screen share), never to protect an actual secret.
- **`AesConfigProtector`** (`Name` = `"AES256"`): AES encryption with a key and IV you supply.

> **Security note: read before using `AesConfigProtector` in production.** The key and IV are derived by right-padding your input strings and truncating to 32 bytes (key) and 16 bytes (IV). They are **not** run through a key-derivation function, and the same fixed IV is reused for every value encrypted by a given instance. AES is used in CBC mode (the .NET default), and reusing an IV across multiple encryptions of related plaintext is a known weakness: it can leak whether two encrypted values share a common prefix. Treat `AesConfigProtector` as "better than Base64, not as strong as a proper secrets manager". Prefer a real key vault or managed secret store for anything sensitive in production. If you do use it, supply a high-entropy, random `encryptionKey`/`iv` pair generated once and stored outside source control.

To add your own (for example, one backed by DPAPI or a key vault), implement `IConfigProtector` and give it a unique `Name`.

### Encrypt values into a JSON file

`EncryptJsonOnBuild` walks a JSON file and replaces the targeted values with their encrypted form:

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

`EncryptJsonOnBuild` rewrites the file in place. It does not register the value into the returned `IConfigurationBuilder`. Values that already carry the configured `Signature` prefix are left untouched, so running it twice won't double-encrypt.

### Decrypt values when building configuration

`DecryptJsonOnBuild` adds a configuration source that decrypts every matching value as the configuration is built:

```csharp
var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .DecryptJsonOnBuild(contract, new List<IConfigProtector> { aes, new Base64ConfigProtector() })
    .Build();
```

Internally this builds an intermediate `IConfiguration` from the sources registered so far, then walks every leaf value. If a value starts with `contract.Signature`, it's split on `contract.Delimiter` into `{signature+version}{delimiter}{providerName}{delimiter}{payload}`, and the protector whose `Name` matches decrypts the payload. Values that don't match the signature pass through unchanged.

Pass every protector you used to encrypt the file, and use the same `ConfigProtectorContract` settings for both steps.

## API reference

```csharp
public interface IConfigProtector
{
    string Name { get; }
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}

public sealed class Base64ConfigProtector : IConfigProtector   // Name => "BASE64"

public sealed class AesConfigProtector : IConfigProtector      // Name => "AES256"
{
    public AesConfigProtector(string encryptionKey, string iv);   // throws ArgumentNullException if either is null/whitespace
    public string Encrypt(string plainText);
    public string Decrypt(string cipherText);
}

public sealed class ConfigProtectorContract(char delimiter = ':')
{
    public char Delimiter { get; }
    public string Signature { get; init; } = "ENC";   // must not contain the delimiter
    public string Version { get; init; } = "v1";      // must not contain the delimiter

    public string BuildPrefix(IConfigProtector protector);
    // => $"{Signature}{Version}{Delimiter}{protector.Name}{Delimiter}", e.g. "ENCv1:AES256:"
}

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

// ConfigProtectorExtensions
IConfigurationBuilder EncryptJsonOnBuild(this IConfigurationBuilder builder, ConfigProtectorContract contract, Action<EncryptionOptions> configure);
IConfigurationBuilder DecryptJsonOnBuild(this IConfigurationBuilder builder, ConfigProtectorContract contract, List<IConfigProtector> protectors);
```

`Encrypt`/`Decrypt` on both built-in protectors pass a null or empty string through unchanged.

## Error handling

Unlike the rest of Core, this pipeline **throws** at configuration-build time, because a broken configuration should stop startup:

- A malformed encrypted-looking value throws `FormatException`.
- An unrecognized protector name throws `KeyNotFoundException`.

## Best practices

- Don't use `AesConfigProtector` as a substitute for a real secrets manager in production. See the security note above.
- Keep encryption keys, IVs and connection strings out of source control, whichever `IConfigProtector` you use.
- For credentials used by the REST or SQL clients, consider [Credential Management](../CredentialMgmt/README.md) instead.
