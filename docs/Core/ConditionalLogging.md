# Conditional logging

Part of [Integration.DevKit.Core](README.md).

`CreateConditionalLogger` creates an `ILogger` that checks a flag on every log call. Every DevKit module uses it to support its `EnableLogging` setting: a module keeps one logger for its whole lifetime, and logging can still be turned on or off at runtime without detaching the app's `ILoggerFactory`.

You can use it the same way in your own classes. For how DevKit logging fits together, see [Logging](../logging.md).

## Included types

| Type | Namespace | What it does |
| --- | --- | --- |
| `LoggerFactoryExtensions` | `Integration.DevKit.Core.Logging` | `CreateConditionalLogger` extension method on `ILoggerFactory`. |

The logger it returns is an internal wrapper. Its `IsEnabled` returns `true` only when your flag is `true` **and** the underlying logger is enabled for that level, and `Log` writes only when `IsEnabled` is `true`.

## How to

### Add a runtime on/off switch to your own logging

```csharp
using Integration.DevKit.Core.Logging;
using Microsoft.Extensions.Logging;

public class MyService
{
    private readonly ILogger? _logger;

    public bool EnableLogging { get; set; } = true;

    public MyService(ILoggerFactory? loggerFactory = null)
    {
        _logger = loggerFactory.CreateConditionalLogger(nameof(MyService), () => EnableLogging);
    }

    public void DoWork()
    {
        _logger?.LogInformation("Working");   // skipped while EnableLogging is false
    }
}
```

The `isEnabled` delegate runs on every log call, so keep it cheap, for example reading a property.

## API reference

```csharp
public static class LoggerFactoryExtensions
{
    // Returns null if factory is null.
    public static ILogger? CreateConditionalLogger(this ILoggerFactory? factory, string categoryName, Func<bool> isEnabled);
}
```

## Best practices

- Store the result in a nullable field and log with `_logger?.`, because the method returns `null` when no `ILoggerFactory` is supplied.
