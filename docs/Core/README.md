# Integration.DevKit.Core

The foundation package every other Integration.DevKit module depends on: shared result types, configuration protection, file/JSON/dictionary utilities, and a lightweight on-demand host.

## What it is

`Integration.DevKit.Core` has no dependency on any other DevKit package. Its classes are grouped by purpose, and each group has its own page:

| Group | Main types | What it's for |
| --- | --- | --- |
| [Result types](ResultTypes.md) | `OperationResult<T>`, `NullableOperationResult<T>`, `NullOperationResult`, `ApiOperationResult<T>` | The result-object pattern used across the entire SDK instead of exceptions for expected failures. |
| [Configuration protection](ConfigurationProtection.md) | `IConfigProtector`, `AesConfigProtector`, `Base64ConfigProtector`, `ConfigProtectorContract`, `EncryptionOptions` | Encrypting and decrypting values inside `appsettings.json`. |
| [On-demand hosting](OnDemandHosting.md) | `OnDemandHost` | A lightweight service container/host for apps not built on `Microsoft.Extensions.Hosting`. |
| [File and directory utilities](FileAndDirectoryUtils.md) | `FileUtils`, `DirectoryUtils` | File and directory operations with path validation and result objects. |
| [JSON utilities](JsonUtils.md) | `JsonUtils`, `JsonExtractionLayout` | Serialization and pulling values out of loosely-structured JSON by key path. |
| [Dictionary utilities](DictionaryUtils.md) | `DictionaryUtils` | Reading values from the dictionary trees `JsonUtils` produces. |
| [Conditional logging](ConditionalLogging.md) | `LoggerFactoryExtensions` | Loggers that can be turned on and off at runtime. Powers every module's `EnableLogging` setting. |
| [Miscellaneous helpers](MiscHelpers.md) | `GCManager`, `MiscUtils` | Throttled garbage collection and Unix-to-Central time conversion. |

## Why it exists

Every DevKit module needs the same building blocks: a way to report failure without throwing, safe file access with path validation, and a way to pull a few values out of loosely-structured JSON. Putting these in one small package means every module behaves the same way — once you know how to check an `OperationResult<T>`, you know how to handle failures anywhere in the SDK — and none of the modules re-implement their own helpers.

Core is also useful on its own: the result types and utilities have no dependency on the rest of the SDK, and `OnDemandHost` lets apps without a generic host (console tools, plugins, legacy code) still use DevKit's DI-based modules.

## Requirements & installation

- `Microsoft.Extensions.Configuration` (for the configuration-protection pipeline)
- `Microsoft.Extensions.Hosting.Abstractions` (for `OnDemandHost` and `IHostApplicationLifetime`)

NuGet: [Integration.DevKit.Core](https://www.nuget.org/packages/Integration.DevKit.Core)

```bash
dotnet add package Integration.DevKit.Core
```

Every other DevKit project already references Core transitively, so you rarely need to add it explicitly unless you're consuming Core's utilities on their own.

## Getting started

Core has no `Add*`/`Initialize` step — its types are used directly:

```csharp
using Integration.DevKit.Core;

var written = FileUtils.WriteToFile(path, content);
if (!written.MethodSuccess)
{
    logger.LogError(written.Exception, "Failed to write {Path}", path);
    return;
}
```

## Configuration

Core binds no configuration section of its own. It does provide the [configuration protection](ConfigurationProtection.md) pipeline for encrypting values in your own `appsettings.json`.

## How to

Each group's page has its own how-to guides:

- [Check a result, or return one from your own method](ResultTypes.md#how-to)
- [Encrypt and decrypt values in `appsettings.json`](ConfigurationProtection.md#how-to)
- [Run DevKit modules without a generic host](OnDemandHosting.md#how-to)
- [Read, write, copy and move files; work with directories](FileAndDirectoryUtils.md#how-to)
- [Pull values out of JSON](JsonUtils.md#how-to)
- [Read values from dictionary trees](DictionaryUtils.md#how-to)
- [Add a runtime on/off switch to your own logging](ConditionalLogging.md#how-to)
- [Force a throttled garbage collection; convert Unix time](MiscHelpers.md#how-to)

## API reference

| Group | API reference |
| --- | --- |
| Result types | [ResultTypes.md](ResultTypes.md#api-reference) |
| Configuration protection | [ConfigurationProtection.md](ConfigurationProtection.md#api-reference) |
| On-demand hosting | [OnDemandHosting.md](OnDemandHosting.md#api-reference) |
| File and directory utilities | [FileAndDirectoryUtils.md](FileAndDirectoryUtils.md#api-reference) |
| JSON utilities | [JsonUtils.md](JsonUtils.md#api-reference) |
| Dictionary utilities | [DictionaryUtils.md](DictionaryUtils.md#api-reference) |
| Conditional logging | [ConditionalLogging.md](ConditionalLogging.md#api-reference) |
| Miscellaneous helpers | [MiscHelpers.md](MiscHelpers.md#api-reference) |

## Error handling

Core is where the result-object pattern is defined — see [Result types](ResultTypes.md). Utility methods return a failed result rather than throwing for expected failures (invalid path, missing file, unparseable JSON). The configuration-protection pipeline is the exception: it throws `FormatException`/`KeyNotFoundException` at configuration-build time for malformed or unrecognized encrypted values, since a broken configuration should stop startup.

## Best practices

- Always check `MethodSuccess` before reading `Result`.
- See each group's page for its own best practices, e.g. [Configuration protection](ConfigurationProtection.md#best-practices) and [File and directory utilities](FileAndDirectoryUtils.md#best-practices).

## Changelog

See [CHANGELOG.md](CHANGELOG.md).
