# JSON utilities

Part of [Integration.DevKit.Core](README.md).

`JsonUtils` provides serialization plus a path-based extraction API for pulling specific values or sub-trees out of loosely-structured JSON, without modeling the whole payload as a C# type. It's useful when consuming a third-party API response where you only need a few fields.

The results are `Dictionary<string, object>` trees, which you can navigate with [`DictionaryUtils`](DictionaryUtils.md).

## Included types

Both live in the `Integration.DevKit.Core` namespace:

| Type | What it does |
| --- | --- |
| `JsonUtils` | Serialize/deserialize, and extract values, lists and sub-trees by key path. |
| `JsonExtractionLayout` | Controls the shape of extracted results: flat from the nearest shared parent, or the full tree from the root. |

## How it works

- **Key paths** use dot notation (`"data.activities.name"`), including numeric indices for array elements (`"data.activities.0.id"`).
- **Layout:** `ExtractParent` (default) returns a flat structure starting at the closest shared parent container. `PreserveRoot` rebuilds the full nested tree from the document root.
- **Nulls:** `removeNulls` (default `true`) drops null values from the result.
- **On failure**, the failed `OperationResult<T>` still holds a usable empty `Dictionary<string, object>` or `List<T>`, so callers can iterate the result without an extra null check.

## How to

### Pull one object out of a response

```csharp
using Integration.DevKit.Core;

var result = JsonUtils.GetDictionary(responseJson, "data.user");
if (result.MethodSuccess)
{
    var name = DictionaryUtils.GetValue<string>(result.Result, "profile.displayName", "Unknown");
}
```

### Pull several fields at once

```csharp
var fields = JsonUtils.GetDictionary(responseJson, new List<string>
{
    "data.user.id",
    "data.user.email",
});
```

### Get a list

```csharp
var ids = JsonUtils.GetList<int>(responseJson, "data.items.id");
var items = JsonUtils.GetDictionaryList(responseJson, "data.items");
```

### Serialize and deserialize

```csharp
var json = JsonUtils.SerializeObjectToJson(myObject);   // indented output
var obj = JsonUtils.DeserializeJsonToObject(json.Result);
```

## API reference

```csharp
public static class JsonUtils
{
    NullableOperationResult<object?> DeserializeJsonToObject(string json);
    OperationResult<string> SerializeObjectToJson(object obj);   // indented output

    OperationResult<Dictionary<string, object>> GetDictionary(string json, string keyPath, JsonExtractionLayout layout = JsonExtractionLayout.ExtractParent, bool removeNulls = true);
    OperationResult<Dictionary<string, object>> GetDictionary(string json, List<string>? keyPaths = null, JsonExtractionLayout layout = JsonExtractionLayout.ExtractParent, bool removeNulls = true);

    OperationResult<List<T>> GetList<T>(string json, string keyPath, JsonExtractionLayout layout = JsonExtractionLayout.ExtractParent, bool removeNulls = true);
    OperationResult<List<T>> GetList<T>(string json, List<string>? keyPaths = null, JsonExtractionLayout layout = JsonExtractionLayout.ExtractParent, bool removeNulls = true);

    OperationResult<List<Dictionary<string, object>>> GetDictionaryList(string json, string keyPath, JsonExtractionLayout layout = JsonExtractionLayout.ExtractParent, bool removeNulls = true);
    OperationResult<List<Dictionary<string, object>>> GetDictionaryList(string json, List<string>? keyPaths = null, JsonExtractionLayout layout = JsonExtractionLayout.ExtractParent, bool removeNulls = true);

    OperationResult<T> ParseAndFilterJson<T>(string rawJson, List<string>? keys = null, JsonExtractionLayout layout = JsonExtractionLayout.ExtractParent, bool removeNulls = true);

    object? ConvertJsonElementToNativeObject(JsonElement element);
}

public enum JsonExtractionLayout
{
    ExtractParent,  // flat structure starting at the closest shared parent container
    PreserveRoot    // reconstructs the full nested tree from the document root
}
```

## Error handling

Methods return a failed result instead of throwing when the JSON can't be parsed. The failed result's `Result` is still an empty collection, never `null`.

## Best practices

- Extract only the fields you need. For payloads you use in full, a typed model with `System.Text.Json` is clearer.
- Use [`DictionaryUtils.GetValue<T>`](DictionaryUtils.md) with a default to read values from the extracted dictionary safely.
