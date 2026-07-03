using Microsoft.VisualBasic;
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
    /// Extracts a single JSON target or merges multiple paths into a strongly-typed <see cref="Dictionary{String, Object}"/>.
    /// Automatically converts JSON arrays or primitive scalar values into wrapped dictionary shapes if requested.
    /// </summary>
    /// <param name="json">The raw JSON string to parse.</param>
    /// <param name="keyPath">The key or dot-notation path.</param>
    /// <param name="removeNulls">If <see langword="true"/>, automatically strips out fields with <see langword="null"/> values from the final structure; otherwise, preserves them. Defaults to <see langword="true"/>.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the extracted dictionary, or an empty <see cref="Dictionary{String, Object}"/> fallback on failure.</returns>
    public static OperationResult<Dictionary<string, object>> GetDictionary(string json, string keyPath, bool removeNulls = true)
    {
        return ParseAndFilterJson<Dictionary<string, object>>(json, new List<string> { keyPath }, removeNulls);
    }

    /// <summary>
    /// Extracts and merges multiple dot-notation structural paths into a single, cohesive <see cref="Dictionary{String, Object}"/>.
    /// </summary>
    /// <param name="json">The raw JSON string to parse.</param>
    /// <param name="keyPaths">A collection of key or dot-notation paths.</param>
    /// <param name="removeNulls">If <see langword="true"/>, automatically strips out fields with <see langword="null"/> values from the final structure; otherwise, preserves them. Defaults to <see langword="true"/>.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the merged dictionary layout, or an empty <see cref="Dictionary{String, Object}"/> fallback on failure.</returns>
    public static OperationResult<Dictionary<string, object>> GetDictionary(string json, List<string>? keyPaths = null, bool removeNulls = true)
    {
        return ParseAndFilterJson<Dictionary<string, object>>(json, keyPaths, removeNulls);
    }

    /// <summary>
    /// Extracts a flat collection of items from a targeted path as a <see cref="List{T}"/>.
    /// </summary>
    /// <typeparam name="T">The primitive or object type of the elements inside the list.</typeparam>
    /// <param name="json">The raw JSON string to parse.</param>
    /// <param name="keyPath">The key or dot-notation path.</param>
    /// <param name="removeNulls">If <see langword="true"/>, automatically strips out fields with <see langword="null"/> values from the final structure; otherwise, preserves them. Defaults to <see langword="true"/>.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the typed collection, or an empty initialized <see cref="List{T}"/> fallback on failure.</returns>
    public static OperationResult<List<T>> GetList<T>(string json, string keyPath, bool removeNulls = true)
    {
        return ParseAndFilterJson<List<T>>(json, new List<string> { keyPath }, removeNulls);
    }

    /// <summary>
    /// Extracts elements across multiple paths, aggregating them into a unified <see cref="List{T}"/>.
    /// </summary>
    /// <typeparam name="T">The type of elements inside the collection.</typeparam>
    /// <param name="json">The raw JSON string to parse.</param>
    /// <param name="keyPaths">A collection of key or dot-notation paths.</param>
    /// <param name="removeNulls">If <see langword="true"/>, automatically strips out fields with <see langword="null"/> values from the final structure; otherwise, preserves them. Defaults to <see langword="true"/>.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the aggregated list, or an empty initialized <see cref="List{T}"/> fallback on failure.</returns>
    public static OperationResult<List<T>> GetList<T>(string json, List<string>? keyPaths = null, bool removeNulls = true)
    {
        return ParseAndFilterJson<List<T>>(json, keyPaths, removeNulls);
    }

    /// <summary>
    /// Extracts a collection of JSON objects from a single target path, converting them safely into a <see cref="List{Dictionary{String, Object}}"/>.
    /// </summary>
    /// <param name="json">The raw JSON string to parse.</param>
    /// <param name="keyPath">The key or dot-notation path.</param>
    /// <param name="removeNulls">If <see langword="true"/>, automatically strips out fields with <see langword="null"/> values from the final structure; otherwise, preserves them. Defaults to <see langword="true"/>.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the list of dictionaries, or an empty <see cref="List{Dictionary{String, Object}}"/> fallback on failure.</returns>
    public static OperationResult<List<Dictionary<string, object>>> GetDictionaryList(string json, string keyPath, bool removeNulls = true)
    {
        return GetList<Dictionary<string, object>>(json, keyPath, removeNulls);
    }

    /// <summary>
    /// Extracts elements across multiple paths, aggregating them into a unified <see cref="List{Dictionary{String, Object}}"/>.
    /// </summary>
    /// <param name="json">The raw JSON string to parse.</param>
    /// <param name="keyPaths">A collection of key or dot-notation paths.</param>
    /// <param name="removeNulls">If <see langword="true"/>, automatically strips out fields with <see langword="null"/> values from the final structure; otherwise, preserves them. Defaults to <see langword="true"/>.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the processed list of dictionaries, or an empty <see cref="List{Dictionary{String, Object}}"/> fallback on failure.</returns>
    public static OperationResult<List<Dictionary<string, object>>> GetDictionaryList(string json, List<string>? keyPaths = null, bool removeNulls = true)
    {
        return GetList<Dictionary<string, object>>(json, keyPaths, removeNulls);
    }

    /// <summary>
    /// Parses a JSON string and extracts specific values. 
    /// Supports both simple keys and deep-path dot-notation (e.g., "User.Profile.Name").
    /// </summary>
    /// <typeparam name="T">The expected return type of the resulting data.</typeparam>
    /// <param name="rawJson">The raw JSON string to process.</param>
    /// <param name="keys">Optional list of keys or paths to extract. If null, the root object is returned.</param>
    /// <param name="removeNulls">If <see langword="true"/>, automatically strips out fields with <see langword="null"/> values from the final structure; otherwise, preserves them. Defaults to <see langword="true"/>.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the extracted data.</returns>
    public static OperationResult<T> ParseAndFilterJson<T>(string rawJson, List<string>? keys = null, bool removeNulls = true)
    {
        var result = new OperationResult<T>();

        try
        {
            if (string.IsNullOrWhiteSpace(rawJson))
            {
                throw new ArgumentException("JSON string cannot be null or empty.", nameof(rawJson));
            }

            var parseResult = InternalParseAndFilterJson(rawJson, keys, out var commonParentPath);
            if (!parseResult.MethodSuccess)
            {
                throw parseResult.Exception!;
            }

            var parsedObject = parseResult.Result;

            // Target requested a single Dictionary and matched a Dictionary
            if (typeof(T) == typeof(Dictionary<string, object>) && parsedObject is Dictionary<string, object?> looseDict)
            {
                var stronglyTypedDict = ConvertToStronglyTypedDictionary(looseDict, removeNulls);
                return result.SetMethodSuccess((T)(object)stronglyTypedDict);
            }

            // Target requested a single Dictionary, but path points to a List/Array
            if (typeof(T) == typeof(Dictionary<string, object>) && parsedObject is List<object?> looseListForDict)
            {
                // Clean up the list internal elements first
                var cleanedList = new List<object?>();
                foreach (var item in looseListForDict)
                {
                    if (item is Dictionary<string, object?> itemDict)
                    {
                        cleanedList.Add(ConvertToStronglyTypedDictionary(itemDict, removeNulls));
                    }
                    else
                    {
                        cleanedList.Add(item);
                    }
                }

                // Determine the correct property name key
                string fallbackKey = "items";

                // 1. Check if we used a common parent path prefix (e.g., "data.activities")
                if (!string.IsNullOrEmpty(commonParentPath))
                {
                    var parentSegments = commonParentPath.Split('.', StringSplitOptions.RemoveEmptyEntries);
                    if (parentSegments.Length > 0)
                    {
                        fallbackKey = parentSegments[^1]; // Captures "activities"
                    }
                }
                // 2. Fall back to evaluating the first single key path if no common parent prefix exists
                else if (keys != null && keys.Count > 0)
                {
                    var pathSegments = keys[0].Split('.', StringSplitOptions.RemoveEmptyEntries);
                    fallbackKey = pathSegments.Length > 0 ? pathSegments[^1] : "items";
                }

                // Synthesise the wrapper dictionary
                var synthesisedDict = new Dictionary<string, object>
                    {
                        { fallbackKey, cleanedList }
                    };

                return result.SetMethodSuccess((T)(object)synthesisedDict);
            }

            // Target requested a single Dictionary, but path pointed to a primitive value or string
            if (typeof(T) == typeof(Dictionary<string, object>) && parsedObject is not null &&
                (parsedObject is string || parsedObject is not System.Collections.IEnumerable))
            {
                // Extract the last property name from the path to act as our dictionary key (e.g., "id" from "data.activities.0.id")
                var pathSegments = keys![0].Split('.', StringSplitOptions.RemoveEmptyEntries);
                var fallbackKey = pathSegments.Length > 0 ? pathSegments[^1] : "value";

                // Build a single-property dictionary containing the primitive value
                var synthesisedDict = new Dictionary<string, object>
                    {
                        { fallbackKey, parsedObject }
                    };

                return result.SetMethodSuccess((T)(object)synthesisedDict);
            }

            // Target requested a Dictionary List, and matched an actual array of items
            if (typeof(T) == typeof(List<Dictionary<string, object>>) && parsedObject is List<object?> looseList)
            {
                var stronglyTypedList = new List<Dictionary<string, object>>();
                foreach (var item in looseList)
                {
                    if (item is Dictionary<string, object?> looseDict1)
                    {
                        var stronglyTypedDict = ConvertToStronglyTypedDictionary(looseDict1, removeNulls);
                        stronglyTypedList.Add(stronglyTypedDict);
                    }
                }
                return result.SetMethodSuccess((T)(object)stronglyTypedList);
            }

            // Target requested a List of Dictionaries, but path navigation pulled back a Dictionary
            if (typeof(T) == typeof(List<Dictionary<string, object>>) && parsedObject is Dictionary<string, object?> targetedSingleDict)
            {
                var stronglyTypedDict = ConvertToStronglyTypedDictionary(targetedSingleDict, removeNulls);

                var wrappedList = new List<Dictionary<string, object>> { stronglyTypedDict };
                return result.SetMethodSuccess((T)(object)wrappedList);
            }

            // Target requested a List of Dictionaries, but path pointed to a primitive value
            if (typeof(T) == typeof(List<Dictionary<string, object>>) && parsedObject is not null &&
                (parsedObject is string || parsedObject is not System.Collections.IEnumerable))
            {
                // Extract the last property name from the path to act as our dictionary key (e.g., "id" from "data.activities.0.id")
                var pathSegments = keys![0].Split('.', StringSplitOptions.RemoveEmptyEntries);
                var fallbackKey = pathSegments.Length > 0 ? pathSegments[^1] : "value";

                // Build a single-property dictionary containing the primitive value
                var synthesisedDict = new Dictionary<string, object>
                    {
                        { fallbackKey, parsedObject }
                    };

                var wrappedList = new List<Dictionary<string, object>> { synthesisedDict };
                return result.SetMethodSuccess((T)(object)wrappedList);
            }


            var tmp = parsedObject is List<object?>;

            // Target is requested a flat List of type T, but path pointed to a primitive value
            if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>))
            {
                var underlyingType = typeof(T).GetGenericArguments()[0];
                var typedListInstance = (System.Collections.IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(underlyingType))!;

                if (parsedObject is not null)
                {
                    if (parsedObject is System.Collections.IEnumerable enumerableObj && parsedObject is not string)
                    {
                        // Case A: It's an iterable collection of items (but not a string)
                        foreach (var item in enumerableObj)
                        {
                            if (item != null)
                            {
                                typedListInstance.Add(Convert.ChangeType(item, underlyingType));
                            }
                            else if (!removeNulls)
                            {
                                // Keeps a null item placeholder intact inside the primitive collection
                                typedListInstance.Add(null);
                            }
                        }
                    }
                    else
                    {
                        // Case B: It's a single standalone primitive value (e.g., an int or string)
                        typedListInstance.Add(Convert.ChangeType(parsedObject, underlyingType));
                    }
                }

                return result.SetMethodSuccess((T)typedListInstance);
            }

            // Fallback direct matching cast
            if (parsedObject is not T typedValue)
            {
                throw new Exception($"Result tree layout could not be safely cast to target type {typeof(T).Name}");
            }

            return result.SetMethodSuccess(typedValue);
        }
        catch (Exception ex)
        {
            // Check if T is a generic List<> (like List<int> or List<Dictionary<string, object>>)
            if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(List<>))
            {
                // Instantiates a fresh instance of whatever List<T> type was requested
                var emptyList = (T)Activator.CreateInstance(typeof(T))!;
                return result.SetMethodFailure(ex, emptyList);
            }

            // Check if T is our standard Dictionary type
            if (typeof(T) == typeof(Dictionary<string, object>))
            {
                var emptyDict = (T)(object)new Dictionary<string, object>();
                return result.SetMethodFailure(ex, emptyDict);
            }

            return result.SetMethodFailure(ex);
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

    private static NullableOperationResult<object?> InternalParseAndFilterJson(string rawJSON, List<string>? keys, out string? commonParentPath)
    {
        var result = new NullableOperationResult<object?>();
        commonParentPath = null; // Initialize the out parameter immediately

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

            // CASE A: Pure Single-Path Extraction
            if (keys.Count == 1)
            {
                var targetElement = GetValueByPath(doc.RootElement, keys[0]);

                if (targetElement == null || targetElement.Value.ValueKind == JsonValueKind.Null)
                {
                    throw new Exception($"Path '{keys[0]}' not found in the JSON structure.");
                }

                var parsedObject = ConvertJsonElementToNativeObject(targetElement.Value);
                return result.SetMethodSuccess(parsedObject);
            }

            // 2. CASE B: Try to find a common parent path prefix pointing to a JSON Object
            string[]? commonSegments = null;

            foreach (var key in keys)
            {
                var segments = key.Split('.', StringSplitOptions.RemoveEmptyEntries);
                if (commonSegments == null)
                {
                    // On the first pass, assume the path is the commonSegment
                    commonSegments = segments.ToArray();
                }
                else
                {
                    // Narrow down to the intersection of segments
                    int matchingCount = 0;
                    for (int i = 0; i < Math.Min(commonSegments.Length, segments.Length - 1); i++)
                    {
                        if (commonSegments[i] == segments[i]) matchingCount++;
                        else break;
                    }
                    commonSegments = commonSegments.Take(matchingCount).ToArray();
                }
            }

            commonParentPath = string.Join(".", commonSegments ?? Array.Empty<string>());

            if (!string.IsNullOrEmpty(commonParentPath))
            {
                var parentElement = GetValueByPath(doc.RootElement, commonParentPath);

                if (parentElement != null)
                {
                    // 1: Common Parent is a JSON Object
                    if (parentElement.Value.ValueKind == JsonValueKind.Object)
                    {
                        var filteredDict = new Dictionary<string, object?>();

                        foreach (var key in keys)
                        {
                            var relativePath = key.Substring(commonParentPath.Length).TrimStart('.');

                            if (string.IsNullOrWhiteSpace(relativePath))
                            {
                                var fullParentNative = ConvertJsonElementToNativeObject(parentElement.Value);
                                if (fullParentNative is Dictionary<string, object?> baseDict)
                                {
                                    foreach (var kvp in baseDict)
                                    {
                                        filteredDict[kvp.Key] = kvp.Value;
                                    }
                                }
                                continue;
                            }

                            var relativeSegments = relativePath.Split('.', StringSplitOptions.RemoveEmptyEntries);
                            MergePathIntoDictionary(parentElement.Value, relativePath, relativeSegments, 0, filteredDict);
                        }

                        return result.SetMethodSuccess(filteredDict);
                    }

                    // 2: Common Parent is a JSON Array
                    if (parentElement.Value.ValueKind == JsonValueKind.Array)
                    {
                        var filteredList = new List<object?>();

                        // Loop through every item inside the targeted array element
                        foreach (var arrayItem in parentElement.Value.EnumerateArray())
                        {
                            var filteredItemDict = new Dictionary<string, object?>();

                            foreach (var key in keys)
                            {
                                var relativePath = key.Substring(commonParentPath.Length).TrimStart('.');

                                // If they just targeted the array itself with no child properties, grab the whole native item
                                if (string.IsNullOrWhiteSpace(relativePath))
                                {
                                    filteredList.Add(ConvertJsonElementToNativeObject(arrayItem));
                                    filteredItemDict = null; // Mark as handled so we don't double-add below
                                    break;
                                }

                                var relativeSegments = relativePath.Split('.', StringSplitOptions.RemoveEmptyEntries);

                                // If it's a wildcard array field expansion mapping (e.g. key was "data.activities.id" -> relative is "id")
                                // Run our recursive builder locally on this specific item row!
                                MergePathIntoDictionary(arrayItem, relativePath, relativeSegments, 0, filteredItemDict);
                            }

                            if (filteredItemDict != null)
                            {
                                filteredList.Add(filteredItemDict);
                            }
                        }

                        return result.SetMethodSuccess(filteredList);
                    }
                }
            }

            // CASE C: Multi-Path Structural Re-creation Fallback
            var extracted = new Dictionary<string, object?>();
            foreach (var key in keys)
            {
                // We use a recursive helper to build/merge the paths into our extracted dictionary
                MergePathIntoDictionary(root, key, key.Split('.', StringSplitOptions.RemoveEmptyEntries), 0, extracted);
            }

            return result.SetMethodSuccess(extracted);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex, null);
        }
    }

    private static void MergePathIntoDictionary(JsonElement currentElement, string fullPath, string[] segments, int segmentIndex, Dictionary<string, object?> currentDict)
    {
        if (segmentIndex >= segments.Length) return;

        var segment = segments[segmentIndex];

        // If the JSON element at this level is an object, navigate down normally
        if (currentElement.ValueKind == JsonValueKind.Object && currentElement.TryGetProperty(segment, out var nextElement))
        {
            // If we are at the final leaf node, assign the value
            if (segmentIndex == segments.Length - 1)
            {
                currentDict[segment] = ConvertJsonElementToNativeObject(nextElement);
                return;
            }

            // If the next tier is an array in the source JSON, we need special handling
            if (nextElement.ValueKind == JsonValueKind.Array)
            {
                // Check if the NEXT segment is an explicit integer index (e.g., "activities.1.id")
                if (segmentIndex + 1 < segments.Length && int.TryParse(segments[segmentIndex + 1], out int explicitIndex))
                {
                    if (explicitIndex >= 0 && explicitIndex < nextElement.GetArrayLength())
                    {
                        // If the explicit array tracker doesn't exist yet, create it
                        if (!currentDict.TryGetValue(segment, out var existingArrayObj) || existingArrayObj is not List<object?> targetList)
                        {
                            targetList = new List<object?>();
                            currentDict[segment] = targetList;
                        }

                        // Pad the list with nulls up to the index if necessary
                        while (targetList.Count <= explicitIndex)
                        {
                            targetList.Add(null);
                        }
                        
                        var targetArrayItem = nextElement[explicitIndex];

                        // FIX: If the explicit index is the absolute leaf node of the path (e.g., "data.activities.0")
                        // map the full item object right here and complete this branch
                        if (segmentIndex + 1 == segments.Length - 1)
                        {
                            targetList[explicitIndex] = ConvertJsonElementToNativeObject(targetArrayItem);
                            return;
                        }

                        // Ensure there's a dictionary placeholder at that index
                        if (targetList[explicitIndex] == null || targetList[explicitIndex] is not Dictionary<string, object?> itemDict)
                        {
                            itemDict = new Dictionary<string, object?>();
                            targetList[explicitIndex] = itemDict;
                        }
                        else
                        {
                            itemDict = (Dictionary<string, object?>)targetList[explicitIndex]!;
                        }

                        // Skip the index segment in the next recursion step since we processed it here
                        MergePathIntoDictionary(targetArrayItem, fullPath, segments, segmentIndex + 2, itemDict);
                    }
                    return;
                }

                // --- Fallback: Wildcard Projection (e.g., "activities.id" -> maps to all items) ---
                if (!currentDict.TryGetValue(segment, out var bulkArrayObj) || bulkArrayObj is not List<object?> bulkList)
                {
                    bulkList = new List<object?>();
                    currentDict[segment] = bulkList;
                }

                int arrayIndex = 0;
                foreach (var arrayItem in nextElement.EnumerateArray())
                {
                    if (bulkList.Count <= arrayIndex)
                    {
                        bulkList.Add(new Dictionary<string, object?>());
                    }

                    if (bulkList[arrayIndex] is Dictionary<string, object?> itemDict)
                    {
                        MergePathIntoDictionary(arrayItem, fullPath, segments, segmentIndex + 1, itemDict);
                    }

                    arrayIndex++;
                }
                return;
            }

            // Standard object nesting fallback
            if (!currentDict.TryGetValue(segment, out var existingNode) || existingNode is not Dictionary<string, object?> nextDict)
            {
                nextDict = new Dictionary<string, object?>();
                currentDict[segment] = nextDict;
            }

            MergePathIntoDictionary(nextElement, fullPath, segments, segmentIndex + 1, nextDict);
        }
    }

    private static JsonElement? GetValueByPath(JsonElement element, string path)
    {
        var keys = path.Split('.', StringSplitOptions.RemoveEmptyEntries);

        for (int i = 0; i < keys.Length; i++)
        {
            var key = keys[i];

            if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty(key, out var next))
            {
                element = next;
            }
            else if (element.ValueKind == JsonValueKind.Array)
            {
                // If we are pointing directly to an index (e.g., "data.activities.0")
                if (int.TryParse(key, out int index))
                {
                    if (index >= 0 && index < element.GetArrayLength())
                    {
                        element = element[index];
                    }
                    else
                    {
                        return null;
                    }
                }
                else
                {
                    // CRITICAL FIX: The user is trying to project a property out of an array of objects 
                    // (e.g., "data.activities.name"). We re-join the remaining path segments and pass them down.
                    var remainingPath = string.Join(".", keys.Skip(i));
                    return ProjectArrayElements(element, remainingPath);
                }
            }
            else
            {
                return null;
            }
        }

        return element;
    }

    private static JsonElement? ProjectArrayElements(JsonElement arrayElement, string subPath)
    {
        using var stream = new System.IO.MemoryStream();
        using (var writer = new Utf8JsonWriter(stream))
        {
            writer.WriteStartArray();
            foreach (var item in arrayElement.EnumerateArray())
            {
                var target = GetValueByPath(item, subPath);
                if (target.HasValue)
                {
                    target.Value.WriteTo(writer);
                }
            }
            writer.WriteEndArray();
        }

        // Parse it back into a detached JsonDocument
        var doc = JsonDocument.Parse(stream.ToArray());
        return doc.RootElement.Clone(); // Clone to keep alive outside the using scope
    }

    private static Dictionary<string, object> ConvertToStronglyTypedDictionary(Dictionary<string, object?> looseDict, bool removeNulls)
    {
        var target = new Dictionary<string, object>();
        foreach (var kvp in looseDict)
        {
            if (kvp.Value is Dictionary<string, object?> nestedLoose)
            {
                target[kvp.Key] = ConvertToStronglyTypedDictionary(nestedLoose, removeNulls);
            }
            else if (kvp.Value is List<object?> nestedList)
            {
                // CRITICAL FIX: Convert arrays found inside the object tree layout
                target[kvp.Key] = ConvertToStronglyTypedList(nestedList, removeNulls);
            }
            else if (kvp.Value != null)
            {
                target[kvp.Key] = kvp.Value;
            }

            // If removeNulls is false, preserve null keys in the dictionary
            else if (!removeNulls)
            {
                target[kvp.Key] = null!;
            }
        }
        return target;
    }

    private static List<object> ConvertToStronglyTypedList(List<object?> looseList, bool removeNulls)
    {
        var targetList = new List<object>();
        foreach (var item in looseList)
        {
            if (item is Dictionary<string, object?> itemDict)
            {
                targetList.Add(ConvertToStronglyTypedDictionary(itemDict, removeNulls));
            }
            else if (item is List<object?> subList)
            {
                targetList.Add(ConvertToStronglyTypedList(subList, removeNulls));
            }
            else if (item != null)
            {
                targetList.Add(item);
            }

            // If removeNulls is false, preserve null keys in the dictionary
            else if (!removeNulls)
            {
                targetList.Add(null!);
            }
        }
        return targetList;
    }
}
