/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using AndrewM5.DevKit.Core.Results;

namespace AndrewM5.DevKit.Core;

/// <summary>
/// Utility methods for navigating, filtering, and transforming complex 
/// Dictionary and List structures, specifically targeting <see cref="Dictionary{TKey, TValue}"/> 
/// and <see cref="List{T}"/> where the values are loosely typed as <see cref="object"/>.
/// </summary>
public static class DictionaryUtils
{
    /// <summary>
    /// Recursively traverses an object structure following a specified path of keys.
    /// </summary>
    /// <remarks>
    /// If a <see cref="List{object}"/> is encountered during traversal, the method branches and 
    /// continues the search for the current path index across all items in that list.
    /// </remarks>
    /// <param name="curObject">The root object (usually a Dictionary or List) to begin traversal.</param>
    /// <param name="path">An ordered array of strings representing the keys to follow.</param>
    /// <returns>
    /// An <see cref="OperationResult{T}"/> containing a list of all objects found at the end of the path.
    /// </returns>
    public static OperationResult<List<object>> TraverseByPath(object curObject, string[] path)
    {
        var result = new OperationResult<List<object>>();
        var matches = new List<object>();

        try
        {
            TraverseInternal(curObject, path, 0, ref matches);

            return result.SetMethodSuccess(matches);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Creates a subset of dictionaries by filtering each entry in the source list to only include specified keys.
    /// </summary>
    /// <param name="source">The source list of dictionaries to process.</param>
    /// <param name="keys">The list of keys to retain in the resulting dictionaries.</param>
    /// <returns>
    /// An <see cref="OperationResult{T}"/> containing a new list of dictionaries, 
    /// each containing only the intersection of the requested keys and the available data.
    /// </returns>
    public static OperationResult<List<Dictionary<string, object>>> ExtractSubsetByKeys(List<Dictionary<string, object>> source, List<string> keys)
    {
        var result = new OperationResult<List<Dictionary<string, object>>>();

        List<Dictionary<string, object>> resultList = new List<Dictionary<string, object>>();
        HashSet<string> keySet = new HashSet<string>(keys);

        try
        {
            if (source == null || keys == null)
            {
                return result.SetMethodSuccess(resultList);
            }


            foreach (var originalDict in source)
            {
                if (originalDict == null || originalDict.Count == 0)
                {
                    continue;
                }

                Dictionary<string, object> filteredDict = new Dictionary<string, object>(keySet.Count);

                foreach (string key in keySet)
                {
                    if (originalDict.TryGetValue(key, out var value))
                    {
                        filteredDict[key] = value;
                    }
                }

                if (filteredDict.Count > 0)
                {
                    resultList.Add(filteredDict);
                }
            }

            return result.SetMethodSuccess(resultList);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Safely retrieves the first dictionary from a list.
    /// </summary>
    /// <param name="dictionaries">The list of dictionaries.</param>
    /// <returns>
    /// An <see cref="OperationResult{T}"/> containing the dictionary at index 0. 
    /// If the list is null, empty, or the first element is null, returns a new empty dictionary.
    /// </returns>
    public static OperationResult<Dictionary<string, object>> GetFirstDictionary(List<Dictionary<string, object>> dictionaries)
    {
        var result = new OperationResult<Dictionary<string, object>>();

        if (dictionaries != null && dictionaries.Count > 0 && dictionaries[0] != null)
        {
            return result.SetMethodSuccess(dictionaries[0]);
        }

        return result.SetMethodSuccess(new Dictionary<string, object>());
    }

    /// <summary>
    /// Locates a nested collection by key and flattens its contents into a single dictionary.
    /// </summary>
    /// <remarks>
    /// If the value at <paramref name="searchKey"/> is a <see cref="List{object}"/>, 
    /// the method iterates through the list and merges all key-value pairs from any contained dictionaries into the result.
    /// Duplicate keys will be overwritten by the last occurrence found.
    /// </remarks>
    /// <param name="source">The source dictionary containing the nested data.</param>
    /// <param name="searchKey">The key used to locate the nested dictionary or list.</param>
    /// <returns>
    /// An <see cref="OperationResult{T}"/> containing a flattened dictionary of all merged key-value pairs.
    /// </returns>
    public static OperationResult<Dictionary<string, object>> FlattenListByKey(Dictionary<string, object> source, string searchKey)
    {
        var result = new OperationResult<Dictionary<string, object>>();
        var resultDict = new Dictionary<string, object>();

        if (source == null || string.IsNullOrEmpty(searchKey))
        {
            return result.SetMethodSuccess(resultDict);
        }

        if (!source.TryGetValue(searchKey, out var value) || value == null)
        {
            return result.SetMethodSuccess(resultDict);
        }

        if (value is Dictionary<string, object> subDict)
        {
            foreach (var keyValuePair in subDict)
            {
                resultDict[keyValuePair.Key] = keyValuePair.Value;
            }
        }
        else if (value is List<object> subList)
        {
            foreach (object item in subList)
            {
                if (item is Dictionary<string, object> dictionary)
                {
                    foreach (var keyValuePair in dictionary)
                    {
                        resultDict[keyValuePair.Key] = keyValuePair.Value;
                    }
                }
            }
        }

        return result.SetMethodSuccess(resultDict);
    }

    /// <summary>
    /// Retrieves a value and attempts to cast or convert it to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The target type for the value.</typeparam>
    /// <param name="dict">The dictionary to search.</param>
    /// <param name="key">The key to look up.</param>
    /// <param name="defaultValue">The value to return if the key is missing, the value is null, or conversion fails.</param>
    /// <returns>The value converted to <typeparamref name="T"/>, or <paramref name="defaultValue"/>.</returns>
    /// <remarks>
    /// This method first attempts a direct cast. If that fails, it attempts to use <see cref="Convert.ChangeType(object, Type)"/>.
    /// </remarks>
    public static T GetValueOrDefault<T>(Dictionary<string, object> dict, string key, T defaultValue)
    {
        if (dict.TryGetValue(key, out var value) && value != null)
        {
            if (value is T result)
            {
                return result;
            }

            try
            {
                // Attempts to convert the object to the target type T
                return (T)Convert.ChangeType(value, typeof(T));
            }
            catch
            {
                return defaultValue;
            }
        }

        return defaultValue;
    }

    /// <summary>
    /// Safely retrieves a nested <see cref="Dictionary{string, object}"/>.
    /// </summary>
    /// <param name="source">The parent dictionary.</param>
    /// <param name="key">The key identifying the nested dictionary.</param>
    /// <returns>The nested dictionary if it exists and matches the type; otherwise, <see langword="null"/>.</returns>
    public static Dictionary<string, object>? GetDictionary(Dictionary<string, object> source, string key)
    {
        if (!source.TryGetValue(key, out var value))
        {
            return null;
        }

        if (value is not Dictionary<string, object> nested)
        {
            return null;
        }

        return nested;
    }

    /// <summary>
    /// Retrieves a list of dictionaries associated with a specific key.
    /// </summary>
    /// <param name="source">The source dictionary.</param>
    /// <param name="key">The key identifying the list.</param>
    /// <returns>
    /// A list of dictionaries if found. Returns <see langword="null"/> if the key is missing, 
    /// the value is not a list, or the list contains no valid dictionaries.
    /// </returns>
    public static List<Dictionary<string, object>>? GetListDictionary(Dictionary<string, object> source, string key)
    {
        if (!source.TryGetValue(key, out var value))
        {
            return null;
        }

        if (value is not List<object> list)
        {
            return null;
        }

        var result = new List<Dictionary<string, object>>(list.Count);

        foreach (var item in list)
        {
            if (item is Dictionary<string, object> dictionary)
            {
                result.Add(dictionary);
            }
        }

        if (result.Count <= 0)
        {
            return null;
        }

        return result;
    }

    /// <summary>
    /// Safely retrieves a <see cref="List{object}"/> associated with the specified key.
    /// </summary>
    /// <param name="source">The source dictionary.</param>
    /// <param name="key">The key identifying the list.</param>
    /// <returns>The list if found and of the correct type; otherwise, <see langword="null"/>.</returns>
    public static List<object>? GetList(Dictionary<string, object> source, string key)
    {
        if (!source.TryGetValue(key, out var value))
        {
            return null;
        }

        if (value is not List<object> list)
        {
            return null;
        }

        return list;
    }

    /// <summary>
    /// Core recursive engine for path traversal.
    /// </summary>
    /// <param name="current">The current level's object context.</param>
    /// <param name="path">The full array of keys to navigate.</param>
    /// <param name="depth">The current index within the <paramref name="path"/>.</param>
    /// <param name="matches">The collection being populated with found leaf-node objects.</param>
    private static void TraverseInternal(object current, string[] path, int depth, ref List<object> matches)
    {
        if (depth == path.Length)
        {
            matches.Add(current);
            return;
        }

        string key = path[depth];

        if (current is Dictionary<string, object> dictionary && dictionary.TryGetValue(key, out var next))
        {
            TraverseInternal(next, path, depth + 1, ref matches);
        }
        else if (current is List<object> list)
        {
            foreach (var item in list)
            {
                // Note: We do not increment depth here because we are searching 
                // for the SAME key across all items in the current list.
                TraverseInternal(item, path, depth, ref matches);
            }
        }
    }
}
