# Result types

Part of [Integration.DevKit.Core](README.md).

Nearly every public method across the SDK returns a result object instead of throwing for expected failures. The caller checks `MethodSuccess`, then reads `Result` or `Exception`. Once you know how to handle one result, you can handle failures anywhere in the SDK.

## Included types

All types live in the `Integration.DevKit.Core` namespace and implement `IOperationResult<T>`:

| Type | Use when | `Result` may be `null`? |
| --- | --- | --- |
| `OperationResult<T>` | The operation produces a real value on success. | No. Assigning a `null` result via `SetMethodSuccess` automatically converts the object to a failure. |
| `NullableOperationResult<T>` | The operation may legitimately succeed with no value. | Yes. |
| `NullOperationResult` | The operation has no payload at all, only success or failure. | N/A (`Result` is always `object?`). |
| `ApiOperationResult<T>` | An HTTP call. Adds `RequestUrl`, `StatusCode`, `ResponseBody` and `DisplaySummary` on top of `NullableOperationResult<T>`. | Yes. |

`ApiOperationResult<T>` is covered in detail in [REST API Management](../RESTApiMgmt/README.md).

## How to

### Check a result

Check `MethodSuccess` before trusting `Result`:

```csharp
var written = FileUtils.WriteToFile(path, content);
if (!written.MethodSuccess)
{
    logger.LogError(written.Exception, "Failed to write {Path}", path);
    return;
}
```

### Return a result from your own method

```csharp
public OperationResult<Customer> LoadCustomer(int id)
{
    var result = new OperationResult<Customer>();
    try
    {
        return result.SetMethodSuccess(repository.Get(id));
    }
    catch (Exception ex)
    {
        return result.SetMethodFailure(ex);
    }
}
```

Use `NullOperationResult` when there's nothing to return (`result.SetMethodSuccess()` takes no argument), and `NullableOperationResult<T>` when "no value" is a valid success.

## API reference

```csharp
public interface IOperationResult<T>
{
    bool MethodSuccess { get; }
    Exception Exception { get; }   // never null; defaults to new Exception("No Error") when unset
    T Result { get; }
}

// OperationResult<T>
result.SetMethodSuccess(value);                    // null value turns the result into a failure
result.SetMethodFailure(ex, defaultValueOnFailure);

// NullableOperationResult<T> / NullOperationResult / ApiOperationResult<T> follow the same pattern,
// with SetMethodSuccess accepting null and NullOperationResult.SetMethodSuccess() taking no argument.
```

## Best practices

- Always check `MethodSuccess` before reading `Result`. Don't rely on `Result` being a sensible default on failure unless that particular method guarantees one.
- Log or return `Exception` on failure rather than discarding it; it carries the original cause.
