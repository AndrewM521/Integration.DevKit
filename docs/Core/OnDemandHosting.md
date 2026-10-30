# On-demand hosting

Part of [Integration.DevKit.Core](README.md).

`OnDemandHost` is a minimal static host for applications that want DevKit's DI-based modules (task management, SQL, etc.) without building a full `Microsoft.Extensions.Hosting` generic host. It exists for console tools, plugins and legacy code that don't otherwise have a hosting/DI pipeline.

Most applications that already use `Host.CreateDefaultBuilder` do **not** need it.

## Included types

All types live in the `Integration.DevKit.Core.OnDemand` namespace:

| Type | What it does |
| --- | --- |
| `OnDemandHost` | Static entry point: register services, start, stop. |
| `OnDemand_Registry` | Holds the shared `IServiceCollection` and builds the `IServiceProvider`. |
| `OnDemand_Lifetime` | Minimal `IHostApplicationLifetime` implementation, so modules that need one (such as Task Management) work without a generic host. |

## How it works

`ConfigureServices` registers `OnDemand_Lifetime` as the app's `IHostApplicationLifetime` (unless one is already registered), then adds your services to a shared, static `IServiceCollection` (`OnDemand_Registry.ServiceCollection`). `Start`/`StartAsync`:

1. Builds the service provider.
2. Registers a shutdown hook on `AppDomain.CurrentDomain.ProcessExit`.
3. Starts every registered `IHostedService`.

Calling `Start` again while the host is running does nothing.

`Stop`/`StopAsync` signals `OnDemand_Lifetime` and stops those hosted services.

## How to

### Run DevKit modules without a generic host

```csharp
using Integration.DevKit.Core.OnDemand;

OnDemandHost.ConfigureServices(services =>
{
    services.AddSQLMgmt(configuration);
    services.AddTaskMgmt(configuration);
});

await OnDemandHost.StartAsync(configuration);

Service_SQLMgmt.Initialize(OnDemandHost.Services);
Service_TaskMgmt.Initialize(OnDemandHost.Services);

// later
await OnDemandHost.StopAsync();
```

Call `ConfigureServices` before `Start`/`StartAsync`. `Services` throws until the host has started.

## API reference

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

public static class OnDemand_Registry
{
    public static IServiceCollection ServiceCollection { get; }
    public static IServiceProvider GetServiceProvider(IConfiguration? configuration = null, bool forceRebuild = false);
}

public class OnDemand_Lifetime : IHostApplicationLifetime
```

## Best practices

- Use `OnDemandHost` only when your app has no generic host. If it already uses `Host.CreateDefaultBuilder`, register modules there instead.
- Call `Stop`/`StopAsync` on shutdown so hosted services and running tasks stop cleanly. The `ProcessExit` hook is a fallback, not a replacement.
