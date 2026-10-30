# Integration.DevKit.CredentialMgmt.Contracts

The `ISecretReader` and `ISecretStore` interfaces that DevKit modules use to read and write secrets, without depending on any storage implementation.

## What it is

This package contains only two interfaces, in the `Integration.DevKit.CredentialMgmt.Contracts` namespace:

- `ISecretReader` — read-only access to secrets, grouped by container.
- `ISecretStore` — extends `ISecretReader` with write and delete operations.

The implementations (`FileSecretStore`, `ConfigurationSecretReader`, `CompositeSecretReader`) live in [Integration.DevKit.CredentialMgmt](../CredentialMgmt/README.md).

## Why it exists

Other modules — [SQLMgmt](../SQLMgmt/README.md) and [RESTApiMgmt](../RESTApiMgmt/README.md) — and their tests need to accept "a secret store" without pulling in ASP.NET Core Data Protection or the file store. Keeping the interfaces in their own small package lets them do that, and lets you write your own secret source (a cloud vault, a database) against the contract alone. This is the only DevKit module that still ships a separate `.Contracts` package; the rest fold their interfaces into the main package.

## Requirements & installation

- [Integration.DevKit.Core](../Core/README.md) (pulled in automatically)

NuGet: [Integration.DevKit.CredentialMgmt.Contracts](https://www.nuget.org/packages/Integration.DevKit.CredentialMgmt.Contracts)

```bash
dotnet add package Integration.DevKit.CredentialMgmt.Contracts
```

You rarely reference this package directly — it comes in with CredentialMgmt, SQLMgmt, and RESTApiMgmt. Reference it on its own only when writing a secret source that shouldn't depend on the full CredentialMgmt package.

## Getting started

There's nothing to register. Implement or accept the interfaces:

```csharp
using Integration.DevKit.CredentialMgmt.Contracts;

public class MyService(ISecretReader secrets)
{
    public string? GetApiKey() =>
        secrets.GetKey("Api", "ApiKey") is { MethodSuccess: true } r ? r.Result : null;
}
```

## Configuration

None.

## How to

### Choose between `ISecretReader` and `ISecretStore`

`ISecretReader` is the minimal, read-only contract — it's what env vars, `IConfiguration`, and read-only cloud-vault views can implement in a handful of lines, without taking on write support they can't (or shouldn't) provide. `ISecretStore` extends it with the write operations, for backends that actually own their storage.

Think of `fileName` as a **container name** — a logical grouping of related secrets (e.g. `"Api"`, `"Database"`) — and `key` as the individual secret's name within that container.

### Implement your own

Implement `ISecretReader` directly for a read-only source. For a read/write store, subclass `SecretStoreBase` from CredentialMgmt (it handles encryption for you) rather than implementing `ISecretStore` from scratch. Worked examples for both are in [CredentialMgmt → Implement your own secret source](../CredentialMgmt/README.md#implement-your-own-secret-source).

## API reference

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
    NullOperationResult DeleteKey(string fileName, string key);     // idempotent
    NullOperationResult DeleteSecret(string fileName);              // idempotent
}
```

## Error handling

Implementations should return a failed result rather than throw. By convention, `GetKey` for a missing key returns a failed result wrapping a `KeyNotFoundException`, and `DeleteKey`/`DeleteSecret` succeed when the key or container doesn't exist.

## Best practices

- `ISecretStore` implementations must encrypt data at rest and restrict access to the underlying storage.
- Depend on `ISecretReader` wherever you only need to read — it keeps callers usable with read-only sources like configuration.

## Changelog

See [CHANGELOG.md](CHANGELOG.md).
