# On-demand hosting

Part of [Integration.DevKit.Core](README.md).

`OnDemandHost` is a minimal host for applications that want DevKit's DI-based modules (task management, SQL, etc.) without building a full `Microsoft.Extensions.Hosting` generic host. It exists for console tools, plugins and legacy code that don't otherwise have a hosting/DI pipeline.

Most applications that already use `Host.CreateDefaultBuilder` do **not** need it.

## Included types

All types live in the `Integration.DevKit.Core.OnDemand` namespace:

| Type | What it does |
| --- | --- |
| `OnDemandHost` | Register services, start, stop. Each instance owns its own service collection and lifetime. |

The host registers its own internal `IHostApplicationLifetime`, so modules that need one (such as Task Management) work without a generic host. Resolve it through the `IHostApplicationLifetime` interface.

## How it works

`ConfigureServices` adds your services to the host's own `IServiceCollection`. `Start`/`StartAsync`:

1. Registers the `configuration` you pass as `IConfiguration`, if none is registered yet.
2. Creates a new `IHostApplicationLifetime` and builds a new service provider.
3. Starts every registered `IHostedService`, one at a time in registration order (like the generic host).
4. Signals `ApplicationStarted`, and registers a shutdown hook on `AppDomain.CurrentDomain.ProcessExit` (once per host).

`Stop`/`StopAsync` signals `ApplicationStopping`, stops the hosted services in reverse order, signals `ApplicationStopped`, and disposes the service provider. That releases singletons such as SQL connections and HTTP clients. A hosted service can also stop the host by calling `IHostApplicationLifetime.StopApplication()`, as with the generic host.

A host can be started again after it was stopped; each run gets a fresh provider and lifetime. Calling `Start` while running, or `Stop` while stopped, does nothing. If a hosted service fails to start, the provider is disposed and the exception is rethrown.

## How to

### Run DevKit modules without a generic host

```csharp
using Integration.DevKit.Core.OnDemand;

await using var host = new OnDemandHost();

host.ConfigureServices(services =>
{
    services.AddSQLMgmt(configuration);
    services.AddTaskMgmt(configuration);
});

await host.StartAsync(configuration);

Service_SQLMgmt.Initialize(host.Services);
Service_TaskMgmt.Initialize(host.Services);

// later
await host.StopAsync();
```

Call `ConfigureServices` before `StartAsync`; registrations added while the host is running take effect on the next start. `Services` throws while the host is not running.

### Restart the host

```csharp
await host.StopAsync();
await host.StartAsync(configuration);

// The old provider was disposed, so re-initialize the static facades from the new one
Service_SQLMgmt.Initialize(host.Services);
Service_TaskMgmt.Initialize(host.Services);
```

## API reference

```csharp
public sealed class OnDemandHost : IAsyncDisposable
{
    public OnDemandHost();

    public IServiceProvider Services { get; }   // throws InvalidOperationException while not running
    public bool IsRunning { get; }

    public OnDemandHost ConfigureServices(Action<IServiceCollection> configureAction);   // returns this, for chaining
    public Task StartAsync(IConfiguration? configuration = null, CancellationToken cancellationToken = default);
    public void Start(IConfiguration? configuration = null);   // blocks on StartAsync
    public Task StopAsync();
    public void Stop();                                        // blocks on StopAsync
    public ValueTask DisposeAsync();                           // stops the host and removes its ProcessExit hook
}
```

## Best practices

- Use `OnDemandHost` only when your app has no generic host. If it already uses `Host.CreateDefaultBuilder`, register modules there instead.
- Call `StopAsync` (or dispose the host) on shutdown so hosted services and running tasks stop cleanly. The `ProcessExit` hook is a fallback, not a replacement.
- After a restart, call each module's `Service_<Module>.Initialize(host.Services)` again: the services from the previous run were disposed.
