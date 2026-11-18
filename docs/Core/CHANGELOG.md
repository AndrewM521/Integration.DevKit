# Changelog — Integration.DevKit.Core

This changelog covers only the `Integration.DevKit.Core` package. See the [root changelog](../CHANGELOG.md) for the full SDK release notes.

## [3.0.0.0] - 2026-11-16 (current)

### Removed
- **Breaking:** `OnDemand_Registry` and the public `OnDemand_Lifetime` class. The host now owns its service collection, and its lifetime is resolved through `IHostApplicationLifetime`.
- **Breaking:** configuration protection is gone: `IConfigProtector`, `AesConfigProtector`, `Base64ConfigProtector`, `ConfigProtectorContract`, `EncryptionOptions`, `JsonEncryptor`, `JsonDecryptor`, `DecryptionConfigSource`/`DecryptionConfigProvider`, and the `EncryptJsonOnBuild`/`DecryptJsonOnBuild` extensions. Use a secret store such as [Credential Management](../CredentialMgmt/README.md) for credentials instead. Values already encrypted with an `ENCv1|...` prefix must be decrypted back to plain values (or moved to a secret store) before upgrading, or they will be read as literal strings.
- **Breaking:** the `allowNoFileExtension` parameter of `FileUtils.DeleteFiles`. It was ignored: matching files are always deleted whether or not they have an extension. Callers that passed it must drop the argument.

### Added
- `DictionaryUtils` key paths can step into lists by position, like `JsonUtils` paths: `GetValue<int>(dict, "orders.0.id")`.

### Fixed
- `FileUtils.WriteToFile`/`WriteToFileAsync` with empty content and `append: false` now truncates the file. Previously it returned success and left the old content in place. Appending empty content is still a no-op.
- `FileUtils.WriteToFile` no longer appends a hard-coded `\` to the parent directory (on Linux this created a folder literally named `\`). Both sync and async writes now handle a bare file name with no directory.
- `OnDemandHost` could not be restarted: `Stop` permanently canceled its static lifetime, so a second `Start` handed every service an already-canceled `ApplicationStopping`. It also never disposed the old service provider (leaking SQL connections and HTTP clients), added another `ProcessExit` handler on every start, and never signaled `ApplicationStarted`. All four are fixed.
- `JsonUtils.GetList<T>`/`ParseAndFilterJson<T>` failed for nullable item types such as `List<int?>`; they now convert to the underlying type. With no keys and a primitive root (e.g. `42`), `ParseAndFilterJson` threw a `NullReferenceException`; it now wraps the value under `"value"`.
- `JsonUtils` and `DictionaryUtils` conversions used the machine's culture, so on e.g. a German-locale server `"1.5"` was read as `15`. They now use the invariant culture.
- `FileUtils.MoveFile`/`CopyFile` decided whether the destination was a folder only from its name, so moving into an existing folder like `v1.2` renamed the file to `v1.2` instead. An existing folder now always counts as a folder.
- `FileUtils.CopyFile`, `MoveFile` and `DeleteFiles` ignored `allowNoFileExtension`, so files like `LICENSE` couldn't be copied or moved, and `DeleteFiles(dir, "*")` failed on them. `DeleteFiles` also created the folder when it didn't exist; it now just returns success.
- `FileUtils.WriteBytesToFile`/`WriteBytesToFileAsync` had the same bugs fixed for text writes: empty content without `append` left the old file in place, and a bare file name with no folder failed. The 100 MB chunk loop is replaced by a single write.
- `JsonUtils.ParseAndFilterJson<T>` for a missing path and a reference type `T` (e.g. `string`) failed with a confusing "Result cannot be null" error. It now fails with a `KeyNotFoundException` naming the path.
- JSON key normalization (renaming `a.b` keys to `a_b`) overwrote an existing `a_b` key in the same object, losing its value. The dotted key is now kept unrenamed instead.
- `FileUtils.WriteToFile`/`WriteToFileAsync` with an empty `string[]` returned without touching the file, so the old content stayed. It now clears the file, like the `string` overloads; with `append` it still does nothing.
- `FileUtils.MoveFile` with `allowNoFileExtension: true` read a new destination name without an extension (e.g. `LICENSE`) as a folder, so the move failed with `DirectoryNotFoundException`. A destination that doesn't exist and doesn't end in a path separator is now the new file name. Existing folders still count as folders.
- `IHostApplicationLifetime.StopApplication()` on an `OnDemandHost` only cancelled `ApplicationStopping`: hosted services were never stopped, `ApplicationStopped` never fired and `IsRunning` stayed `true`. It now stops the host, like the generic host.
- `DictionaryUtils.GetList<int>` (and `GetValue<List<int>>`) threw `ArgumentNullException` when the list held a null, although it promises never to throw. It now returns the default, like any other failed conversion.
- `JsonUtils` paths that pick a property out of every array item (e.g. `data.items.name`) left a parsed document undisposed.
- `DirectoryUtils.CreateDirectory`, `DeleteDirectory`, `GetFiles` and `FileUtils.DeleteFiles` rejected an existing folder whose name has a dot (e.g. `v1.2`) with "Path must be a Directory". An existing folder now always counts as a folder, like the copy and move destinations.
- `JsonUtils.GetDictionary` on a path that points to an array ignored `removeNulls` for the wrapped items: null items and nulls inside nested lists were kept. They are now removed like everywhere else.
- `JsonUtils` read very small numbers as `0`: `TryGetDecimal` accepts `1.23e-30` by rounding it to zero. A `decimal` that hits its 28-digit scale limit (where that rounding happens) now becomes `double`, which keeps the value; every other non-integer still becomes `decimal`.

### Changed
- **Breaking:** `OnDemandHost` is now an instance class (`new OnDemandHost()`, `IAsyncDisposable`) instead of a static class. Each instance has its own service collection and lifetime and can be stopped and started again. Replace `OnDemandHost.ConfigureServices(...)`/`OnDemandHost.StartAsync(...)`/`OnDemandHost.Services` with calls on a host instance you keep for the app's lifetime. See [On-demand hosting](OnDemandHosting.md).
- **Behavior change:** JSON numbers that don't fit `int`/`long` now become `decimal` when they fit, and `double` only beyond `decimal`'s range. Previously they were always `double`, which rounded values like `12345678901234.5678`. Code that casts these values to `double` directly must convert instead (e.g. `Convert.ToDouble`).
- `FileUtils.WriteToFile`/`WriteToFileAsync` write content with a single `StreamWriter.Write` call instead of a per-character chunking loop, which made large writes much slower.
- JSON key normalization now first scans for dotted keys and skips the extra parse/rewrite/serialize work when there are none (the usual case). When there are, it copies each value once instead of deep-copying every subtree at every level.
- `OnDemandHost` now starts hosted services one at a time in registration order and stops them in reverse order, like the generic host. Before, they all started and stopped at once, so a service relying on an earlier one having started could fail at random. If several services fail to stop, the others still stop and the errors are thrown together as an `AggregateException`.
- `GCManager.CallGC_Collect` no longer resumes on the caller's synchronization context, so the REST and SQL sync methods that call it can't deadlock on a UI thread.
- **Behavior change:** `JsonUtils` rejects trailing commas everywhere. Before, JSON with a dotted key name (`{"a.b":1,}`) was parsed leniently, while the same mistake without one (`{"a":1,}`) failed.
- Results rethrown inside the library keep the original stack trace (`ExceptionDispatchInfo.Throw` instead of `throw result.Exception`), so a logged error points at the line that failed, not the one that passed it on. This applies across all modules.
- `FileUtils` and `DirectoryUtils` check paths through one shared helper each instead of 18 copies of the same block. No behavior change.
- **Packaging:** Core references only the packages it uses (`Logging.Abstractions`, `Configuration.Abstractions`, `DependencyInjection`, `Hosting.Abstractions`). It no longer brings `Microsoft.Extensions.Options` and `Options.ConfigurationExtensions`; add them yourself if your code got them through Core.
- The four `FileUtils` write methods share one path-check and folder-creation step, so sync, async, text and byte writes can no longer drift apart. `JsonUtils` names wrapped values (last path segment) in one place. No behavior change.
- Version bumped from `2.1.1.0` to `3.0.0.0`. Major version bump because of the removed API and breaking changes above.

## [2.1.1.0] - 2026-10-30

### Added
- The NuGet package now includes this module's README (`docs/Core/README.md`), shown on the package's nuget.org page.

### Changed
- Version bumped from `2.1.0.0` to `2.1.1.0`.

## [2.1.0.0] - 2026-10-28

### Changed
- Now multi-targets `net8.0`, `net9.0` and `net10.0` (previously `net8.0` only). Building all three targets requires the .NET 10 SDK.
- `Microsoft.Extensions.*` packages bumped to `10.0.14`.

### Notes
- Behavior from the `Microsoft.Extensions.*` 10.x packages applies on every target: configuration binding keeps JSON `null` as `null` instead of `""`.

## [2.0.0.0] - 2026-09-18

### Changed
- Version bumped from `1.2.0.0-preview` to `2.0.0.0`; strict `Major.Minor.Build.0` numbering from here on.
- Unit tests added; setup code split into separate classes; files and folders reorganized.
- NuGet package metadata added.

### Fixed
- Small bug in JSON key normalization.

## [1.0.1.x] - 2026-07-06 to 2026-08-25

### Added
- `JsonUtil` overhauled to be much more generic, with JSON encrypt/decrypt support; keys containing `.` are now normalized, with an option to always create the root structure.
- On-demand initialization for services; utility methods reshuffled between `DictionaryUtil` and `FileUtil`, including a new `ReadBytes` helper.

### Changed
- On-demand initialization moved out of each module into its own custom host (`OnDemandHost`).

## [1.0.0.x] - 2026-06-05 to 2026-06-25

### Added
- First versioned release (`1.0.0.1`). MIT license and license headers added.

### Changed
- Extensions renamed to `Utils`.
- `AndrewM5.DevKit` namespace renamed to `Integration.DevKit`; `appsettings` structure updated to match.
- Namespaces changed to use the root namespace instead of per-folder namespaces.

### Fixed
- Bug in `NullableOperationResult`.

## Pre-1.0 (unversioned) - 2026-01-28 to 2026-06-04

### Added
- Initial core classes.
- Garbage collector helper (`GCManager`).
- `NullOperationResult`/`NullableOperationResult` introduced.

### Fixed
- `CallGC_Collect` could run more than once per second; fixed with a lock and its logger call removed.
