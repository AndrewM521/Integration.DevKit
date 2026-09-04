# Extending DevKit modules

Most modules in this SDK expose exactly one implementation of everything — `ThreadLockManager`, `ProcessManager`, `SQLClient` — as plain concrete classes with no interface at all. Three modules are different: `RESTApiMgmt`, `TaskMgmt`, and `CredentialMgmt` each have a genuine **pluggable extension point** — something a consuming application can swap out or add its own implementation of — and their folder/namespace layout reflects that.

## Why some modules have interfaces and some don't

Every interface and abstract base class in this SDK earns its place by one of two tests:

- **Multiple real implementations exist or are expected.** `IAuthStrategy` ships one implementation (`OAuth2ClientCredentialsAuthStrategy`) but is designed for you to add your own (an API-key scheme, mTLS, whatever your downstream API needs). `IIterationStrategy`/`Time_IterationStrategy` already has three shipped subclasses (`TimeStrategy_Daily`/`Hourly`/`Interval`). `ISecretReader`/`ISecretStore` has two shipped readers plus a store, explicitly designed to be composed and extended.
- **A test needs to substitute it.** `IApiManager` and `IManagedTaskHandle` are mocked in this SDK's own test suite to isolate the class under test from the rest of the runtime.

Everything else — `ThreadLockManager`, `ProcessManager`/`ManagedProcess`, `SQLManager`/`SQLClient`, `TaskManager`/`TaskRegistry`, `ApiClient`/`ApiManager` itself — has exactly one implementation, nothing mocks it, and nothing is ever going to substitute a different one. Those modules expose plain classes with no interface, because the interface would be pure ceremony.

## The folder/namespace convention

For the three modules with a real extension point, each piece lives in a folder (and matching namespace) that tells you what it is:

| Folder | Namespace suffix | What lives here |
| --- | --- | --- |
| `Interfaces/` | `.Interfaces` | The pure interface(s) defining the extension point. |
| `Abstractions/` | `.Abstractions` | Abstract base classes you subclass instead of implementing the interface directly — they handle the boilerplate so your subclass only needs to fill in the one thing that varies. |
| `Implementations/` | `.Implementations` | Concrete, ready-to-use classes this library ships. Use these as-is, or read them as worked examples for writing your own. |
| `Settings/` | `.Settings` | Settings/options POCOs bound from configuration. |

Not every module has all four — a module only gets `Abstractions/`/`Implementations/` if it has something worth extending in the first place:

| Module | `Interfaces/` | `Abstractions/` | `Implementations/` | `Settings/` |
| --- | --- | --- | --- | --- |
| `ThreadLocks` | — | — | — | ✓ |
| `ProcessLauncher` | — | — | — | ✓ |
| `SQLMgmt` | — | — | — | ✓ |
| `RESTApiMgmt` | ✓ | — | ✓ | ✓ |
| `TaskMgmt` | ✓ | ✓ | ✓ | ✓ (plus `Models/` for plain DTOs — `ManagedTaskSettings`, `TimeStrategySettings`) |
| `CredentialMgmt` | — (separate `.Contracts` package) | ✓ | ✓ | ✓ |

`CredentialMgmt` is the one module whose interfaces (`ISecretReader`/`ISecretStore`) still ship as a separate `Integration.DevKit.CredentialMgmt.Contracts` package rather than folded into the main package — that's because it has real consumers (other modules, and their tests) that reference the interfaces without needing the implementation.

## Extension points at a glance

| Module | Extension point | Shipped implementation(s) | Where a custom one goes |
| --- | --- | --- | --- |
| REST API Management | `IAuthStrategy` (`Interfaces/`) | `OAuth2ClientCredentialsAuthStrategy` (`Implementations/`) | Anywhere in your own application — see [Implementing your own `IAuthStrategy`](rest-api.md#implementing-your-own-iauthstrategy). |
| Task Management | `IIterationStrategy` / `Time_IterationStrategy` (`Interfaces/`, `Abstractions/`) | `TimeStrategy_Daily`/`Hourly`/`Interval` (`Implementations/`) | Anywhere in your own application — see [Implementing your own iteration strategy](task-management.md#implementing-your-own-iteration-strategy). |
| Credential Management | `ISecretReader` / `ISecretStore` (in `Integration.DevKit.CredentialMgmt.Contracts`), `SecretStoreBase` (`Abstractions/`) | `ConfigurationSecretReader`, `CompositeSecretReader`, `FileSecretStore` (`Implementations/`) | Anywhere in your own application — see [Implementing your own secret source](credential-management.md#implementing-your-own-secret-source). |

In every case, `Implementations/`/`Abstractions/` are **this library's own internal convention** for organizing the classes it ships — nothing requires a consuming application's custom implementation to live under any particular namespace. Put your `IAuthStrategy`, `IIterationStrategy`, or `ISecretReader`/`ISecretStore` wherever the rest of your application's code lives; the SDK only cares that it implements the interface (or, for `Time_IterationStrategy`/`SecretStoreBase`, that it derives from the right base class).
