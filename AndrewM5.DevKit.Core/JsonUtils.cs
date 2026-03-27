using AndrewM5.DevKit.Core.Results;
using System.Text.Json;

namespace AndrewM5.DevKit.Core;

public static class JsonUtils
{
    public static NullableOperationResult<object?> ConvertJsonToObject(string json)
    {
        var result = new NullableOperationResult<object>();

        try
        {
            using (var doc = JsonDocument.Parse(json))
            {
                object? obj = ConvertJsonElementToNativeObject(doc.RootElement);

                return result.SetMethodSuccess(obj);
            }
        }
        catch (Exception ex) 
        { 
            return result.SetMethodFailure(ex);
        }
    }
    public static OperationResult<string> SerializeObjectToJson(object obj)
    {
        var result = new OperationResult<string>();

        try
        {
            string json = JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = true });

            return result.SetMethodSuccess(json);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }
    public static OperationResult<T> ParseAndFilterJson<T>(string rawJson, List<string>? keys = null)
    {
        var result = new OperationResult<T>();

        try
        {
            var parseResult = InternalParseAndFilterJson(rawJson, keys);
            if (!parseResult.MethodSuccess)
            {
                throw parseResult.Exception;
            }

            if (parseResult.Result is not T typed)
            {
                throw new Exception($"Result is not of type {typeof(T).Name}");
            }

            return result.SetMethodSuccess(typed);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }
    public static OperationResult<Dictionary<string, object>> FilterDictionary(Dictionary<string, object> originalDictionary, List<string>? keys = null)
    {
        var result = new OperationResult<Dictionary<string, object>>();

        try
        {
            var getJson = SerializeObjectToJson(originalDictionary);
            if (!getJson.MethodSuccess)
            {
                throw getJson.Exception;
            }

            var getFilteredDictionary = ParseAndFilterJson<Dictionary<string, object>>(getJson.Result, keys);
            if (!getFilteredDictionary.MethodSuccess)
            {
                throw getFilteredDictionary.Exception;
            }

            return result.SetMethodSuccess(getFilteredDictionary.Result);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }
    public static NullableOperationResult<Dictionary<string, object>> GetDictionary(Dictionary<string, object> dictionary, string keyPath)
    {
        return GetDictionaryValue(dictionary, keyPath, new Dictionary<string, object>());
    }
    public static NullableOperationResult<List<Dictionary<string, object>>> GetListDictionary(Dictionary<string, object> dictionary, string keyPath)
    {
        return GetDictionaryValue(dictionary, keyPath, new List<Dictionary<string, object>>());
    }
    public static NullableOperationResult<T> GetDictionaryValue<T>(Dictionary<string, object> dictionary, string keyPath, T defaultVal = default!)
    {
        var result = new NullableOperationResult<T>();

        if (dictionary == null || string.IsNullOrWhiteSpace(keyPath))
        {
            return result.SetMethodSuccess(defaultVal)!;
        }

        object? currentObj = dictionary;

        var keys = keyPath.Split('.', StringSplitOptions.RemoveEmptyEntries);

        foreach (var key in keys)
        {
            if (currentObj is Dictionary<string, object> currentDict &&
                currentDict.TryGetValue(key, out var next))
            {
                currentObj = next;
            }
            else
            {
                return result.SetMethodSuccess(defaultVal)!;
            }
        }

        if (currentObj is T typed)
        {
            return result.SetMethodSuccess(typed)!;
        }

        try
        {
            if (currentObj == null)
            {
                return result.SetMethodSuccess(defaultVal)!;
            }

            var targetType = typeof(T);
            var underlyingType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            if (underlyingType.IsGenericType && underlyingType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var itemType = underlyingType.GetGenericArguments()[0];

                if (currentObj is IEnumerable<object?> objList)
                {
                    var listType = typeof(List<>).MakeGenericType(itemType);
                    var list = (System.Collections.IList)Activator.CreateInstance(listType)!;

                    foreach (var item in objList)
                    {
                        if (item == null)
                        {
                            list.Add(null);
                            continue;
                        }

                        if (itemType.IsInstanceOfType(item))
                        {
                            list.Add(item);
                            continue;
                        }

                        try
                        {
                            var convertedItem = Convert.ChangeType(item, itemType);
                            list.Add(convertedItem);
                        }
                        catch (Exception ex)
                        {
                            return result.SetMethodFailure(
                                new Exception($"Failed to convert list item to {itemType.Name}", ex)
                            )!;
                        }
                    }

                    return result.SetMethodSuccess((T)list)!;
                }
            }

            var converted = Convert.ChangeType(currentObj, underlyingType);

            return result.SetMethodSuccess((T)converted!)!;
        }
        catch (Exception ex)
        { 
            return result.SetMethodFailure(ex)!;
        }
    }
    public static object? ConvertJsonElementToNativeObject(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var dict = new Dictionary<string, object?>();
                foreach (var prop in element.EnumerateObject())
                {
                    dict[prop.Name] = ConvertJsonElementToNativeObject(prop.Value);
                }
                return dict;

            case JsonValueKind.Array:
                var list = new List<object?>();
                foreach (var item in element.EnumerateArray())
                {
                    list.Add(ConvertJsonElementToNativeObject(item));
                }
                return list;

            case JsonValueKind.String:
                return element.GetString();

            case JsonValueKind.Number:
                if (element.TryGetInt32(out int intVal)) return intVal;
                if (element.TryGetInt64(out long longVal)) return longVal;
                if (element.TryGetDouble(out double doubleVal)) return doubleVal;
                return element.GetRawText(); // Fallback for very large decimals/exponents

            case JsonValueKind.True:
                return true;
            case JsonValueKind.False:
                return false;
            case JsonValueKind.Null:
                return null;

            default:
                return null;
        }
    }


    private static NullableOperationResult<object?> InternalParseAndFilterJson(string rawJSON, List<string>? keys = null)
    {
        var result = new NullableOperationResult<object?>();

        if (string.IsNullOrWhiteSpace(rawJSON))
        {
            return result.SetMethodSuccess(null);
        }

        try
        {
            using var doc = JsonDocument.Parse(rawJSON);
            var root = doc.RootElement;

            if (keys == null || keys.Count == 0)
            {
                var rootObject = ConvertJsonElementToNativeObject(root);

                return result.SetMethodSuccess(rootObject);
            }

            // Root must be an object to extract keys
            if (root.ValueKind != JsonValueKind.Object)
            {
                var rootObject = ConvertJsonElementToNativeObject(root);

                return result.SetMethodSuccess(rootObject);
            }

            var extracted = new Dictionary<string, object?>();

            // Step 1: extract all requested keys
            foreach (var key in keys)
            {
                var element = GetValueByPath(root, key);

                if (element.HasValue)
                {
                    extracted[key] = ConvertJsonElementToNativeObject(element.Value);
                }
            }

            return result.SetMethodSuccess(extracted);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }
    private static JsonElement? GetValueByPath(JsonElement element, string path)
    {
        var keys = path.Split('.', StringSplitOptions.RemoveEmptyEntries);

        foreach (var key in keys)
        {
            if (element.ValueKind == JsonValueKind.Object &&
                element.TryGetProperty(key, out var next))
            {
                element = next;
            }
            else
            {
                return null;
            }
        }

        return element;
    }
}
