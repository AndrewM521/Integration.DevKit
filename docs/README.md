# Integration.DevKit

Integration.DevKit is a .NET 8 SDK made up of small, independently-referenceable modules for the things most integration-style applications end up building anyway: protected configuration, structured logging, REST API access, SQL access, background task management, thread coordination, external process management, and file-based secret storage.

Every module follows the same three conventions, which makes the SDK predictable once you've learned one part of it:

1. **Result objects instead of exceptions for expected failures.** Public methods return `OperationResult<T>` (or `NullOperationResult`, `NullableOperationResult<T>`, `ApiOperationResult<T>`) rather than throwing — check `MethodSuccess` before trusting `Result`. See [Core → Result types](core.md#result-types).
2. **Register, build, initialize.** Each module has an `Add<Module>(...)` extension method called during `ConfigureServices`, and a static `Service_<Module>.Initialize(IServiceProvider)` call made once after the host is built. A few modules have an ordering dependency on another (documented per-module below); most don't.
3. **Named clients/managers where it makes sense.** REST clients, SQL clients, and locks are all looked up by string name/key through a manager rather than constructed directly.

## Modules

| Module | What it's for | NuGet |
| --- | --- | --- |
| [Core](core.md) | Result types, configuration protection, file/directory/JSON/dictionary utilities, on-demand hosting. Depended on by every other module. | [Integration.DevKit.Core](https://www.nuget.org/packages/Integration.DevKit.Core) |
| [REST API Management](rest-api.md) | Named `HttpClient`-backed clients with typed results, metrics, and optional secret-store-backed credentials. | [Integration.DevKit.RESTApiMgmt](https://www.nuget.org/packages/Integration.DevKit.RESTApiMgmt) |
| [SQL Management](sql-management.md) | Named SQL clients, connection testing, command/data-reader helpers. | [Integration.DevKit.SQLMgmt](https://www.nuget.org/packages/Integration.DevKit.SQLMgmt) |
| [Thread Locks &amp; Thread-Safe File I/O](thread-locks.md) | Named sync/async locks; thread-safe file I/O built on top of them. | [Integration.DevKit.ThreadLocks](https://www.nuget.org/packages/Integration.DevKit.ThreadLocks) &middot; [ThreadSafeItems](https://www.nuget.org/packages/Integration.DevKit.ThreadSafeItems) |
| [Task Management](task-management.md) | Recurring/long-running background work with retry policies, iteration timing strategies, and run history. | [Integration.DevKit.TaskMgmt](https://www.nuget.org/packages/Integration.DevKit.TaskMgmt) |
| [Process Launcher](process-launcher.md) | Starting and supervising external processes with output capture and cancellation. | [Integration.DevKit.ProcessLauncher](https://www.nuget.org/packages/Integration.DevKit.ProcessLauncher) |
| [Credential Management](credential-management.md) | File-based secret storage encrypted at rest, pluggable into the REST and SQL clients. | [Integration.DevKit.CredentialMgmt](https://www.nuget.org/packages/Integration.DevKit.CredentialMgmt) &middot; [.Contracts](https://www.nuget.org/packages/Integration.DevKit.CredentialMgmt.Contracts) |

All packages are published under the [AndrewM5 NuGet profile](https://www.nuget.org/profiles/AndrewM5).

Most modules ship their interfaces and settings types as part of the main package itself, organized into `Interfaces/`, `Abstractions/`, `Implementations/`, and `Settings/` folders/namespaces. `CredentialMgmt` is the one exception that still ships a separate `.Contracts` package for its interfaces. See [Extending DevKit modules](extending-modules.md) for what each folder means and how to plug in your own implementation.

## Requirements

- .NET 8 SDK or later
- A host application using `Microsoft.Extensions.Hosting` (or [`OnDemandHost`](core.md#on-demand-hosting) if you don't already have one)

## Installation

Reference the projects you need directly, or consume the published NuGet packages:

```bash
dotnet add reference src/Integration.DevKit.Core/Integration.DevKit.Core.csproj
dotnet add reference src/Integration.DevKit.RESTApiMgmt/Integration.DevKit.RESTApiMgmt.csproj
dotnet add reference src/Integration.DevKit.TaskMgmt/Integration.DevKit.TaskMgmt.csproj
dotnet add reference src/Integration.DevKit.SQLMgmt/Integration.DevKit.SQLMgmt.csproj
```

Every module besides Core is independent of the others at compile time — take only what you need. `ThreadSafeItems` and `CredentialMgmt` are the two exceptions: `ThreadSafeItems` depends on `ThreadLocks` directly, and `CredentialMgmt` depends on `ThreadLocks` too (its `"File"` provider needs `ThreadLockManager` to serialize concurrent access to the same secrets file).

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
| Credential Management | `Integration.DevKit:CredentialManagement` | `Provider` selects the backend; provider-specific settings go under `Options`. See [Credential Management](credential-management.md). |

Every module's settings also include an `EnableLogging` flag (default `true`). Unlike the other settings, it's checked fresh on every log call rather than only at startup — so flipping it on a manager's `RuntimeSettings` at runtime (e.g. `apiManager.RuntimeSettings.EnableLogging = false;`) silences or resumes that module's logging immediately, without detaching the `ILoggerFactory` you registered for the rest of the app.

```json
{
  "Integration.DevKit": {
    "ApiClientManagement": {
      "Default_HttpTimeout_Seconds": 30
    },
    "TaskManagement": {
      "MaxConcurrentTasks": 100
    }
  }
}
```

Values can be encrypted at rest inside the JSON file itself using [configuration protection](core.md#configuration-protection), or you can plug REST/SQL clients into the [file-based secret store](credential-management.md) for credentials specifically.

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

See [Core → Result types](core.md#result-types) for the full set of variants and when each is used.

## Best Practices

- Check `MethodSuccess` before reading `Result` — don't assume a sensible default on failure unless a method's docs say otherwise.
- Follow the register → build → initialize order for every module, respecting the ordering dependencies noted above and on each module's page.
- Keep secrets out of `appsettings.json` in plain text — use [Credential Management](credential-management.md) or, at minimum, [configuration protection](core.md#configuration-protection).
- Use the SDK's logger consistently for diagnostics rather than `Console.WriteLine` in production code paths.

## FAQ

**Can I use only one module?**
Yes — every module besides Core (and `ThreadSafeItems`/`CredentialMgmt`, which both need `ThreadLocks`) is independent. Reference and register only what you need.

**Is this SDK production-ready?**
It provides reusable abstractions and service wiring, but validate configuration against your own runtime requirements before depending on any single module in production.
