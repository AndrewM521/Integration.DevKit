# Miscellaneous helpers

Part of [Integration.DevKit.Core](README.md).

Small standalone helpers that don't belong to a larger group.

## Included types

Both are static classes in the `Integration.DevKit.Core` namespace:

| Type | What it does |
| --- | --- |
| `GCManager` | Throttled, thread-safe forced garbage collection for long-running services. |
| `MiscUtils` | Converts a Unix timestamp to US Central time. |

## How to

### Force a garbage collection only when it's worth it

```csharp
await GCManager.CallGC_Collect("after nightly import");
```

`CallGC_Collect` only collects when **either**:

- the managed heap has grown by more than 20 MB since the last forced collection, or
- more than 20 minutes have passed since the last forced collection.

Otherwise it does nothing. If a call is already in progress, a second concurrent call returns immediately. When it does collect, it runs a full collection across all generations, waits for pending finalizers, and writes the before/after memory (with `description`) to `Debug.WriteLine`.

It's intended for long-running services with bursty allocation patterns, not for general use in request paths.

### Convert a Unix timestamp to Central time

```csharp
DateTime local = MiscUtils.ConvertUnixToCentralTime(1767225600);
```

The correct time zone ID is picked automatically for Windows (`"Central Standard Time"`) and Linux/containers (`"America/Chicago"`).

## API reference

```csharp
public static class GCManager
{
    public static Task CallGC_Collect(string? description = null);
}

public static class MiscUtils
{
    public static DateTime ConvertUnixToCentralTime(long unixSeconds);
}
```

## Best practices

- Don't call `CallGC_Collect` in a hot path. Call it after a large batch of work, where a pause is acceptable.
