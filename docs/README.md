# Integration.DevKit

Integration.DevKit is a .NET 8 SDK made up of small, independently-referenceable modules for the things most integration-style applications end up building anyway: protected configuration, structured logging, REST API access, SQL access, background task management, thread coordination, external process management, and file-based secret storage.

Every module follows the same three conventions, which makes the SDK predictable once you've learned one part of it:

1. **Result objects instead of exceptions for expected failures.** Public methods return `OperationResult<T>` (or `NullOperationResult`, `NullableOperationResult<T>`, `ApiOperationResult<T>`) rather than throwing — check `MethodSuccess` before trusting `Result`. See [Core → Result types](core.md#result-types).
2. **Register, build, initialize.** Each module has an `Add<Module>(...)` extension method called during `ConfigureServices`, and a static `Service_<Module>.Initialize(IServiceProvider)` call made once after the host is built. A few modules have an ordering dependency on another (documented per-module below); most don't.
3. **Named clients/managers where it makes sense.** REST clients, SQL clients, and locks are all looked up by string name/key through a manager rather than constructed directly.

## Modules

| Module | What it's for |
| --- | --- |
| [Core](core.md) | Result types, configuration protection, file/directory/JSON/dictionary utilities, on-demand hosting. Depended on by every other module. |
| [Custom Logging](logging.md) | An `ILogger`-compatible logger with console, debug, and buffered file output, plus a background flush service. |
| [REST API Management](rest-api.md) | Named `HttpClient`-backed clients with typed results, metrics, and optional secret-store-backed credentials. |
| [SQL Management](sql-management.md) | Named SQL clients, connection testing, command/data-reader helpers. |
| [Thread Locks &amp; Thread-Safe File I/O](thread-locks.md) | Named sync/async locks; thread-safe file I/O built on top of them. |
| [Task Management](task-management.md) | Recurring/long-running background work with retry policies, iteration timing strategies, and run history. |
| [Process Launcher](process-launcher.md) | Starting and supervising external processes with output capture and cancellation. |
| [Credential Management](credential-management.md) | File-based secret storage encrypted at rest, pluggable into the REST and SQL clients. |

## Requirements

- .NET 8 SDK or later
- A host application using `Microsoft.Extensions.Hosting` (or [`OnDemandHost`](core.md#on-demand-hosting) if you don't already have one)

## Installation

Reference the projects you need directly, or consume the published NuGet packages:

```bash
dotnet add reference src/Integration.DevKit.Core/Integration.DevKit.Core.csproj
dotnet add reference src/Integration.DevKit.CustomLogger/Integration.DevKit.CustomLogger.csproj
dotnet add reference src/Integration.DevKit.RESTApiMgmt/Integration.DevKit.RESTApiMgmt.csproj
dotnet add reference src/Integration.DevKit.TaskMgmt/Integration.DevKit.TaskMgmt.csproj
dotnet add reference src/Integration.DevKit.SQLMgmt/Integration.DevKit.SQLMgmt.csproj
```

Every module besides Core is independent of the others at compile time — take only what you need. `ThreadSafeItems` is the one exception, since it depends on `ThreadLocks` directly.

## Quick start

```csharp
using Integration.DevKit.CredentialMgmt;
using Integration.DevKit.CustomLogger;
using Integration.DevKit.CustomLogger.Flusher;
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
        services.AddCustomLogging(configuration);
        services.AddCustomLogFlusher(configuration);   // requires AddCustomLogging first
        services.AddThreadLocks();
        services.AddThreadSafeItems();                 // requires AddThreadLocks first
        services.AddRESTApiMgmt(configuration);
        services.AddSQLMgmt(configuration);
        services.AddTaskMgmt(configuration);
        services.AddProcessLauncher();
        services.AddFileSecretStore("MyApp", @"C:\MyApp\Secrets", @"C:\MyApp\Keys");
    });

var app = builder.Build();

// Same ordering rules apply to Initialize as to the Add* calls above.
Service_CustomLogger.Initialize(app.Services);
Service_CustomLogFlusher.Initialize(app.Services);
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

Most modules bind their settings from a section under `Integration.DevKit` in your `IConfiguration` (typically `appsettings.json`); a couple take plain parameters instead. See each module's page for the full settings shape.

| Module | Config section | Notes |
| --- | --- | --- |
| Custom Logging | `Integration.DevKit:CustomLogger` | |
| Custom Logging (flusher) | `Integration.DevKit:CustomLoggerFlusher` | |
| REST API Management | `Integration.DevKit:ApiClientManagement` | |
| SQL Management | `Integration.DevKit:SQLManagement` | |
| Task Management | `Integration.DevKit:TaskManagement` | |
| Thread Locks | — | No settings; `AddThreadLocks()` takes no configuration. |
| Thread-Safe File I/O | — | No settings. |
| Process Launcher | — | No settings; every process is configured per-call. |
| Credential Management | — | Takes `applicationName`/`secretsFolder`/`keysFolder` directly as arguments to `AddFileSecretStore`. |

```json
{
  "Integration.DevKit": {
    "CustomLogger": {
      "OutputLogLevel": "Information"
    },
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

## Known issues

A few real, verified issues are called out in detail on their respective module pages rather than buried here — worth reading before you depend on the affected behavior:

- **REST API Management** — `IApiClient.RuntimeSettings` throws `NotImplementedException` when accessed through the interface type (i.e. straight off `GetClient(...)`). See [REST API Management → Known issue](rest-api.md#known-issue-runtimesettings-throws-through-the-iapiclient-interface).
- **Process Launcher** — the documented "no timeout" default (`TimeoutSeconds = -1` or `0`) is not actually honored by the current implementation and can throw from a background task. See [Process Launcher](process-launcher.md#managedprocessconfig).
- **Custom Logging** — depending on `CreateLogFile`/`AllowCreateFileInContainer`, buffered messages can either accumulate unbounded in memory or be silently dropped. See [Custom Logging → Configuration](logging.md#configuration).

## Best Practices

- Check `MethodSuccess` before reading `Result` — don't assume a sensible default on failure unless a method's docs say otherwise.
- Follow the register → build → initialize order for every module, respecting the ordering dependencies noted above and on each module's page.
- Keep secrets out of `appsettings.json` in plain text — use [Credential Management](credential-management.md) or, at minimum, [configuration protection](core.md#configuration-protection).
- Use the SDK's logger consistently for diagnostics rather than `Console.WriteLine` in production code paths.

## FAQ

**Can I use only one module?**
Yes — every module besides Core (and `ThreadSafeItems`, which needs `ThreadLocks`) is independent. Reference and register only what you need.

**Is this SDK production-ready?**
It provides reusable abstractions and service wiring, but validate configuration and the [known issues](#known-issues) above against your own runtime requirements before depending on any single module in production.
