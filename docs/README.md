# Integration.DevKit

Integration.DevKit is a .NET Core SDK made up of small, independently-referenceable modules for the things most integration-style applications use

Every module follows the same three conventions, which makes the SDK predictable once you've learned one part of it:
1. **Result objects instead of exceptions for expected failures.** Public methods return `OperationResult<T>` (or `NullOperationResult`, `NullableOperationResult<T>`, `ApiOperationResult<T>`) rather than throwing. See [Core → Result types](Core/ResultTypes.md).
2. **Register, build, initialize.** Each module has an `Add<Module>(...)` extension method called during `ConfigureServices`, and a static `Service_<Module>.Initialize(IServiceProvider)` call made once after the host is built. A few modules have an ordering dependency on another (documented per-module below); most don't.
3. **Named clients/managers where it makes sense.** REST clients, SQL clients, and locks are all looked up by string name/key through a manager rather than constructed directly.

See [CHANGELOG.md](CHANGELOG.md) for SDK-wide release history. Each module has its own documents folder `docs/` with a `README.md` and a `CHANGELOG.md`

## Requirements

- .NET Core 8, 9, or 10 SDK
- A `Microsoft.Extensions.DependencyInjection` service container for every module except Core (each module registers with `Add<Module>` and initializes from the built `IServiceProvider`)
- Task Management only: a host that provides `IHostApplicationLifetime`, such as `Microsoft.Extensions.Hosting` (or [`OnDemandHost`](Core/OnDemandHosting.md) if you don't already have one)

## Modules

| Module | What it's for | Changelog | NuGet Package | Install Command |
| --- | --- | --- | --- | --- |
| [Core](Core/README.md) | Result types, configuration protection, file/directory/JSON/dictionary utilities, on-demand hosting. Depended on by every other module. | [Changelog](Core/CHANGELOG.md) | [Integration.DevKit.Core](https://www.nuget.org/packages/Integration.DevKit.Core) | `dotnet add package Integration.DevKit.Core` |
| [REST API Management](RESTApiMgmt/README.md) | Named `HttpClient`-backed clients with typed results, metrics, and a pluggable auth strategy. | [Changelog](RESTApiMgmt/CHANGELOG.md) | [Integration.DevKit.RESTApiMgmt](https://www.nuget.org/packages/Integration.DevKit.RESTApiMgmt) | `dotnet add package Integration.DevKit.RESTApiMgmt` |
| [SQL Management](SQLMgmt/README.md) | Named SQL clients, connection testing, command/data-reader helpers. | [Changelog](SQLMgmt/CHANGELOG.md) | [Integration.DevKit.SQLMgmt](https://www.nuget.org/packages/Integration.DevKit.SQLMgmt) | `dotnet add package Integration.DevKit.SQLMgmt` |
| [Thread Locks](ThreadLocks/README.md) | Named, reference-counted sync/async locks. | [Changelog](ThreadLocks/CHANGELOG.md) | [Integration.DevKit.ThreadLocks](https://www.nuget.org/packages/Integration.DevKit.ThreadLocks) | `dotnet add package Integration.DevKit.ThreadLocks` |
| [Thread-Safe File I/O](ThreadSafeItems/README.md) | File reads/writes serialized per path, built on Thread Locks. | [Changelog](ThreadSafeItems/CHANGELOG.md) | [Integration.DevKit.ThreadSafeItems](https://www.nuget.org/packages/Integration.DevKit.ThreadSafeItems) | `dotnet add package Integration.DevKit.ThreadSafeItems` |
| [Task Management](TaskMgmt/README.md) | Recurring/long-running background work with retry policies, iteration timing strategies, and run history. | [Changelog](TaskMgmt/CHANGELOG.md) | [Integration.DevKit.TaskMgmt](https://www.nuget.org/packages/Integration.DevKit.TaskMgmt) | `dotnet add package Integration.DevKit.TaskMgmt` |
| [Process Launcher](ProcessLauncher/README.md) | Starting and supervising external processes with output capture and cancellation. | [Changelog](ProcessLauncher/CHANGELOG.md) | [Integration.DevKit.ProcessLauncher](https://www.nuget.org/packages/Integration.DevKit.ProcessLauncher) | `dotnet add package Integration.DevKit.ProcessLauncher` |
| [Credential Management](CredentialMgmt/README.md) | File-based secret storage encrypted at rest, pluggable into the REST and SQL clients. | [Changelog](CredentialMgmt/CHANGELOG.md) | [Integration.DevKit.CredentialMgmt](https://www.nuget.org/packages/Integration.DevKit.CredentialMgmt) | `dotnet add package Integration.DevKit.CredentialMgmt` |
| [Credential Management Contracts](CredentialMgmt.Contracts/README.md) | `ISecretReader`/`ISecretStore` interfaces, without the implementation. | [Changelog](CredentialMgmt.Contracts/CHANGELOG.md) | [Integration.DevKit.CredentialMgmt.Contracts](https://www.nuget.org/packages/Integration.DevKit.CredentialMgmt.Contracts) | `dotnet add package Integration.DevKit.CredentialMgmt.Contracts` |

Modules ship their interfaces and settings types as part of the main package, organized into `Interfaces/`, `Abstractions/`, `Implementations/`, and `Settings/` folders/namespaces. See [Extending DevKit modules](extending-modules.md) for what each folder means and how to plug in your own implementation.

## Quick start

```csharp
using Integration.DevKit.CredentialMgmt;
using Integration.DevKit.ProcessLauncher;
using Integration.DevKit.RESTApiMgmt;
using Integration.DevKit.SQLMgmt;
using Integration.DevKit.TaskMgmt;
using Integration.DevKit.ThreadLocks;
using Integration.DevKit.ThreadSafeItems;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // Modules with an ordering dependency are commented; everything else can be in any order.
        services.AddThreadLocks(configuration);
        services.AddThreadSafeItems(configuration);     // requires AddThreadLocks first
        services.AddRESTApiMgmt(configuration);
        services.AddSQLMgmt(configuration);
        services.AddTaskMgmt(configuration);
        services.AddProcessLauncher(configuration);
        services.AddCredentialMgmt(configuration);      // reads Integration.DevKit:CredentialManagement
    });

var app = builder.Build();

// Same ordering rules apply to Initialize as to the Add* calls above.
Service_ThreadLocks.Initialize(app.Services);
Service_ThreadSafeItems.Initialize(app.Services);
Service_RESTApiMgmt.Initialize(app.Services);
Service_SQLMgmt.Initialize(app.Services);
Service_TaskMgmt.Initialize(app.Services);
Service_ProcessLauncher.Initialize(app.Services);
Service_CredentialMgmt.InitializeFileSecretStore(app.Services);

await app.RunAsync();
```

Only register and initialize the modules you actually use — this example includes all of them for illustration.

## Configuration

Every module binds its settings from a section under `Integration.DevKit` in your `IConfiguration` (typically `appsettings.json`). See each module's page for the full settings shape.

| Module | Config section | Notes |
| --- | --- | --- |
| REST API Management | `Integration.DevKit:ApiClientManagement` | |
| SQL Management | `Integration.DevKit:SQLManagement` | |
| Task Management | `Integration.DevKit:TaskManagement` | |
| Thread Locks | `Integration.DevKit:ThreadLocks` | Only setting is `EnableLogging`. |
| Thread-Safe File I/O | `Integration.DevKit:ThreadSafeItems` | Only setting is `EnableLogging`. |
| Process Launcher | `Integration.DevKit:ProcessLauncher` | Only setting is `EnableLogging`; every process is still configured per-call. |
| Credential Management | `Integration.DevKit:CredentialManagement` | `Provider` selects the backend; provider-specific settings go under `Options`. See [Credential Management](CredentialMgmt/README.md). |

Values can be encrypted at rest inside the JSON file itself using [configuration protection](Core/ConfigurationProtection.md), or you can plug REST/SQL clients into the [file-based secret store](CredentialMgmt/README.md) for credentials specifically.

## Error handling

Across every module, prefer checking the result object over wrapping calls in `try`/`catch`:

```csharp
var result = await someOperation();
if (!result.MethodSuccess)
{
    logger.LogError(result.Exception, "Operation failed");
    return;
}

Use(result.Result);
```

See [Core → Result types](Core/ResultTypes.md) for the full set of variants and when each is used.

## Best Practices

- Check `MethodSuccess` before reading `Result` — don't assume a sensible default on failure unless a method's docs say otherwise.
- Follow the register → build → initialize order for every module, respecting the ordering dependencies noted above and on each module's page.
- Keep secrets out of `appsettings.json` in plain text — use [Credential Management](CredentialMgmt/README.md) or, at minimum, [configuration protection](Core/ConfigurationProtection.md).
- Use the SDK's logger consistently for diagnostics rather than `Console.WriteLine` in production code paths — see [Logging](logging.md) for how to plug in your own `ILogger`-based provider.

## FAQ

**Can I use only one module?**
Yes — every module besides Core (and `ThreadSafeItems`/`CredentialMgmt`, which both need `ThreadLocks`) is independent. Reference and register only what you need.

**Is this SDK production-ready?**
It provides reusable abstractions and service wiring, but validate configuration against your own runtime requirements before depending on any single module in production.
