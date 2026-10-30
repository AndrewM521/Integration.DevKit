# Dictionary utilities

Part of [Integration.DevKit.Core](README.md).

`DictionaryUtils` navigates the loosely-typed `Dictionary<string, object>` trees produced by [`JsonUtils`](JsonUtils.md). Every lookup takes a dot-notation key path and returns a default instead of throwing when the path is missing or the value has the wrong type.

## Included types

| Type | Namespace | What it does |
| --- | --- | --- |
| `DictionaryUtils` | `Integration.DevKit.Core` | Read values, nested dictionaries and lists by key path; pick the first dictionary from a list; flatten a nested list into one dictionary. |

## How it works

- **Key paths** are a single key (`"Id"`) or dot notation (`"Meta.Metadata.Id"`).
- **`GetValue<T>`** converts the located value to `T`, including element-by-element conversion for `List<T>`. It returns `defaultValue` if any segment is missing, the value is null, or conversion fails. It never throws.
- **`GetDictionary`, `GetList<T>`, `GetDictionaryList`** return an empty collection when the path is missing or the value is the wrong type.

## How to

### Read a value with a fallback

```csharp
using Integration.DevKit.Core;

var name = DictionaryUtils.GetValue<string>(user, "profile.displayName", "Unknown");
var age = DictionaryUtils.GetValue<int>(user, "profile.age");   // 0 if missing
```

### Read nested objects and lists

```csharp
var address = DictionaryUtils.GetDictionary(user, "profile.address");
var tags = DictionaryUtils.GetList<string>(user, "tags");
var orders = DictionaryUtils.GetDictionaryList(user, "orders");
```

### Take the first item from a list

```csharp
var first = DictionaryUtils.GetFirstDictionary(orders);   // empty dictionary if the list is null or empty
```

### Flatten a nested list into one dictionary

`FlattenListByKey` finds the value at `searchKey`. If it's a list, it merges the key-value pairs from every dictionary in the list into a single result. When keys repeat, the last one wins.

```csharp
var settings = DictionaryUtils.FlattenListByKey(source, "settings");
```

## API reference

```csharp
public static class DictionaryUtils
{
    T GetValue<T>(Dictionary<string, object> dictionary, string keyPath, T defaultValue = default!);
    Dictionary<string, object> GetDictionary(Dictionary<string, object> dictionary, string keyPath);
    List<T> GetList<T>(Dictionary<string, object> dictionary, string keyPath);
    List<Dictionary<string, object>> GetDictionaryList(Dictionary<string, object> dictionary, string keyPath);
    OperationResult<Dictionary<string, object>> GetFirstDictionary(List<Dictionary<string, object>> dictionaries);
    OperationResult<Dictionary<string, object>> FlattenListByKey(Dictionary<string, object> source, string searchKey);
}
```

## Best practices

- Pass an explicit `defaultValue` to `GetValue<T>` when `default(T)` (such as `0` or `false`) could be mistaken for real data.
