using AndrewM5.DevKit.Core.Results;
using System.Text.Json;

namespace AndrewM5.DevKit.Core;

/// <summary>
/// Provides a set of utility methods for handling JSON serialization, 
/// deserialization, and deep-path value extraction.
/// </summary>
public static class JsonUtils
{
    /// <summary>
    /// Converts a JSON string into a native C# object (typically a Dictionary or List).
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <returns>A <see cref="NullableOperationResult{T}"/> containing the parsed object or an exception on failure.</returns>
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

    /// <summary>
    /// Serializes a C# object into an indented JSON string.
    /// </summary>
    /// <param name="obj">The object to serialize.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the JSON string.</returns>
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

    /// <summary>
    /// Parses a JSON string and optionally filters it to include only specific keys before casting to a type.
    /// </summary>
    /// <typeparam name="T">The expected return type.</typeparam>
    /// <param name="rawJson">The raw JSON string.</param>
    /// <param name="keys">Optional list of dot-notation keys to extract. Defaults to null meaning no filter</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the filtered and typed result.</returns>
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

    /// <summary>
    /// Filters an existing Dictionary based on a list of keys, returning a new Dictionary.
    /// </summary>
    /// <param name="source">The source dictionary.</param>
    /// <param name="keys">The list of keys (or paths) to extract. Defaults to null meaning no filter</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the filtered Dictionary.</returns>
    public static OperationResult<Dictionary<string, object>> FilterDictionary(Dictionary<string, object> source, List<string>? keys = null)
    {
        var result = new OperationResult<Dictionary<string, object>>();

        try
        {
            var getJson = SerializeObjectToJson(source);
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

    /// <summary>
    /// Retrieves a nested Dictionary from a source Dictionary
    /// </summary>
    /// <param name="source">The source dictionary.</param>
    /// <param name="keyPath">The key (or path) to the dictionary (e.g., "User.Profile.Settings").</param>
    /// <returns>A <see cref="NullableOperationResult{T}"/> containing the nested Dictionary.</returns>
    public static NullableOperationResult<Dictionary<string, object>> GetDictionary(Dictionary<string, object> source, string keyPath)
    {
        return GetDictionaryValue(source, keyPath, new Dictionary<string, object>());
    }

    /// <summary>
    /// Retrieves a list of Dictionaries from a source Dictionary
    /// </summary>
    /// <param name="dictionary">The source dictionary.</param>
    /// <param name="keyPath">The key (or path) to the dictionary.</param>
    /// <returns>A <see cref="NullableOperationResult{T}"/> containing the list of Dictionaries.</returns>
    public static NullableOperationResult<List<Dictionary<string, object>>> GetListDictionary(Dictionary<string, object> dictionary, string keyPath)
    {
        return GetDictionaryValue(dictionary, keyPath, new List<Dictionary<string, object>>());
    }

    /// <summary>
    /// Navigates a Dictionary by key or path and attempts to convert the value found to type <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The type to convert the found value to.</typeparam>
    /// <param name="dictionary">The source dictionary.</param>
    /// <param name="keyPath">The key or path to the value.</param>
    /// <param name="defaultVal">The default value to return if the path is not found or the dictionary is null.</param>
    /// <returns>A <see cref="NullableOperationResult{T}"/> containing the converted value.</returns>
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

    /// <summary>
    /// Recursively converts a <see cref="JsonElement"/> into native .NET types (Dictionary, List, string, int, etc.).
    /// </summary>
    /// <param name="element">The JSON element to convert.</param>
    /// <returns>The native .NET equivalent of the JSON element.</returns>
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

    /// <summary>
    /// Logic for parsing and extracting specific paths from a JSON string.
    /// </summary>
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

    /// <summary>
    /// Traverses a <see cref="JsonElement"/> tree to find a value at the specified dot-notation path.
    /// </summary>
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
