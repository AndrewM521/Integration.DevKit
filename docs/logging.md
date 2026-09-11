# Logging

DevKit core modules only depend on `Microsoft.Extensions.Logging.Abstractions` (`ILogger`/`ILoggerFactory`) — bring whatever `ILogger`-based logger you already use (Serilog, NLog, `Microsoft.Extensions.Logging`'s built-in console/debug providers, or none at all).

## Bring your own logger

DevKit modules (`ProcessManager`, `ApiManager`, `SQLManager`, `TaskManager`, `ThreadSafeFileIO`, etc.) take an optional `ILoggerFactory?`/`ILogger?` constructor parameter. When they're constructed through DI, any `ILoggerFactory` registered in the container is used automatically — you don't need to pass anything module-by-module. The standard way to get one registered is the framework's own `services.AddLogging(...)`, with your provider of choice added via `AddProvider`/`AddConsole`/or your logging library's own `Add*` extension.

```csharp
var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddLogging(logging =>
        {
            logging.AddConsole();          // or AddProvider(...) for a third-party logger
        });

        services.AddSQLMgmt(configuration);
        services.AddTaskMgmt(configuration);
        // ...any other DevKit modules
    });

var app = builder.Build();

Service_SQLMgmt.Initialize(app.Services);
Service_TaskMgmt.Initialize(app.Services);

await app.RunAsync();
```

Because `Host.CreateDefaultBuilder` already wires up `ILoggerFactory` from every registered `ILoggerProvider`, every DI-constructed DevKit module now logs through whatever provider(s) you registered, with no further per-module wiring.

## Turning logging on or off per module

Every module's settings include an `EnableLogging` flag (default `true`). Unlike the other settings, it's checked fresh on every log call rather than only at startup — so flipping it on a manager's `RuntimeSettings` at runtime (e.g. `apiManager.RuntimeSettings.EnableLogging = false;`) silences or resumes that module's logging immediately, without detaching the `ILoggerFactory` you registered for the rest of the app. See [README → Configuration](README.md#configuration) for the config section each module reads this from.
