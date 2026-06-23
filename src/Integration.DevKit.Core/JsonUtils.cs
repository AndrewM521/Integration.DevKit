using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;

namespace Integration.DevKit.Core;

/// <summary>
/// Utilities for JSON serialization, deserialization, and complex path-based data extraction from <see cref="Dictionary{TKey, TValue}"/> structures.
/// </summary>
public static class JsonUtils
{
    #region Core Serialization
    /// <summary>
    /// Converts a JSON string into native C# objects (typically <see cref="Dictionary{string, Object}"/> or <see cref="List{Object}"/>).
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <returns>A <see cref="NullableOperationResult{T}"/> indicating the status of the operation.</returns>
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
    /// Serializes a C# object into an indented, human-readable JSON string.
    /// </summary>
    /// <param name="obj">The object to serialize.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the formatted JSON string.</returns>
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
    #endregion

    #region Filtering & Path Extraction
    /// <summary>
    /// Parses a JSON string and extracts specific values. 
    /// Supports both simple keys and deep-path dot-notation (e.g., "User.Profile.Name").
    /// </summary>
    /// <typeparam name="T">The expected return type of the resulting data.</typeparam>
    /// <param name="rawJson">The raw JSON string to process.</param>
    /// <param name="keys">Optional list of keys or paths to extract. If null, the entire object is returned.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the extracted data.</returns>
    /// <remarks>
    /// Note: Dot-notation is supported for traversing nested objects but is not required; 
    /// regular top-level keys will work as expected.
    /// </remarks>
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
    /// Filters an existing <see cref="Dictionary{string, Object}"/> by specific keys or paths.
    /// </summary>
    /// <param name="source">The source dictionary.</param>
    /// <param name="keys">The list of keys or paths to extract.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the subset of data.</returns>
    /// <remarks>
    /// This method internally serializes the dictionary to JSON and then uses 
    /// <see cref="ParseAndFilterJson{T}"/> to extract the requested keys.
    /// </remarks>
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
    #endregion

    #region Dictionary Navigation
    /// <summary>
    /// Retrieves a <see cref="List{T}"/> of dictionaries from the source using a key or dot-notation path.
    /// </summary>
    /// <param name="dictionary">The source dictionary.</param>
    /// <param name="keyPath">The key or dot-notation path to the list.</param>
    /// <returns>A <see cref="Dictionary{string, object}"/> or an empty <see cref="Dictionary{string, object}"/>.</returns>
    public static Dictionary<string, object> GetDictionary(Dictionary<string, object> dictionary, string keyPath)
    {
        return GetValue(dictionary, keyPath, new Dictionary<string, object>());
    }

    /// <summary>
    /// Retrieves a <see cref="List{Dictionary{string, object}}"/> from the source using a key or dot-notation path.
    /// </summary>
    /// <param name="dictionary">The source dictionary to search.</param>
    /// <param name="keyPath">The key or dot-notation path (e.g., "Data.Items") to the target list.</param>
    /// <returns>A <see cref="List{Dictionary{string, object}}"/> or an empty <see cref="List{Dictionary{string, object}}"/>.</returns>
    public static List<Dictionary<string, object>> GetListDictionary(Dictionary<string, object> dictionary, string keyPath)
    {
        return GetValue(dictionary, keyPath, new List<Dictionary<string, object>>());
    }

    /// <summary>
    /// Navigates a dictionary by a specific key or a dot-notation path and attempts 
    /// to convert the found value to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The target type for conversion.</typeparam>
    /// <param name="dictionary">The source dictionary.</param>
    /// <param name="keyPath">The key or dot-notation path (e.g., "Id" or "Meta.Metadata.Id").</param>
    /// <param name="defaultValue">Optional value to return if the key is missing, the value is null, or conversion fails.</param>
    /// <returns>The value converted to <typeparamref name="T"/>, or <paramref name="defaultValue"/>.</returns>
    /// <remarks>
    /// This method first attempts a direct cast. If that fails, it attempts to use <see cref="Convert.ChangeType(object, Type)"/>.
    /// </remarks>
    public static T GetValue<T>(Dictionary<string, object> dictionary, string keyPath, T defaultValue = default!)
    {
        if (dictionary == null || string.IsNullOrWhiteSpace(keyPath))
        {
            return defaultValue;
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
                return defaultValue;
            }
        }

        if (currentObj is T typed)
        {
            return typed;
        }

        if (currentObj == null)
        {
            return defaultValue;
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
                        Debug.WriteLine($"Failed to convert list item to {itemType.Name}. {ex}");
                        
                        return defaultValue;
                    }
                }

                return (T)list;
            }
        }

        try
        {
            var converted = Convert.ChangeType(currentObj, underlyingType);

            return (T)converted;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to convert list to {underlyingType.Name}. {ex}");

            return defaultValue;
        }
    }
    #endregion

    #region Helpers
    /// <summary>
    /// Recursively converts a <see cref="JsonElement"/> into its native .NET equivalent.
    /// </summary>
    /// <param name="element">The JSON element to convert.</param>
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

    #endregion
}
