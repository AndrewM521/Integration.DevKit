using System.Collections.Generic;
using System.Diagnostics;
using System.Text.Json;

namespace Integration.DevKit.Core;

/// <summary>
/// Utilities for JSON serialization, deserialization, and complex path-based data extraction from <see cref="Dictionary{TKey, TValue}"/> structures.
/// </summary>
public static class JsonUtils
{
    /// <summary>
    /// Converts a JSON string into native C# objects (typically <see cref="Dictionary{string, Object}"/> or <see cref="List{Object}"/>).
    /// </summary>
    /// <param name="json">The JSON string to parse.</param>
    /// <returns>A <see cref="NullableOperationResult{T}"/> indicating the status of the operation.</returns>
    public static NullableOperationResult<object?> DeserializeJsonToObject(string json)
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

    /// <summary>
    /// Parses a JSON string and extracts specific values. 
    /// Supports both simple keys and deep-path dot-notation (e.g., "User.Profile.Name").
    /// </summary>
    /// <typeparam name="T">The expected return type of the resulting data.</typeparam>
    /// <param name="rawJson">The raw JSON string to process.</param>
    /// <param name="keys">Optional list of keys or paths to extract. If null, the root object is returned.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the extracted data.</returns>
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
    /// Parses a JSON string, navigates to a specific key or dot-notation path, and converts the value to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The target type for conversion.</typeparam>
    /// <param name="json">The JSON string source.</param>
    /// <param name="keyPath">The key or dot-notation path.</param>
    /// <param name="defaultValue">Optional value to return if JSON is invalid, path is missing, value is null, or conversion fails.</param>
    /// <returns>The value converted to <typeparamref name="T"/>, or <paramref name="defaultValue"/>.</returns>
    public static T GetValue<T>(string json, string keyPath, T defaultValue = default!)
    {
        if (string.IsNullOrWhiteSpace(json) || string.IsNullOrWhiteSpace(keyPath))
        {
            return defaultValue;
        }

        try
        {
            // Parse the raw JSON string into a disposable JsonDocument
            using var document = JsonDocument.Parse(json);

            // Navigate the hierarchy using the root element
            var targetElement = GetValueByPath(document.RootElement, keyPath);

            // If path wasn't found or the property is explicitly null in JSON
            if (targetElement == null || targetElement.Value.ValueKind == JsonValueKind.Null)
            {
                return defaultValue;
            }

            // Convert the found JsonElement to the final T type
            var result = JsonSerializer.Deserialize<T>(targetElement.Value.GetRawText());
            if (result == null)
            {
                return defaultValue;
            }

            return result;
        }
        catch (JsonException ex)
        {
            Debug.WriteLine($"Invalid JSON string provided. {ex.Message}");
            return defaultValue;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to convert JSON path to {typeof(T).Name}. {ex}");
            return defaultValue;
        }
    }

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
}
