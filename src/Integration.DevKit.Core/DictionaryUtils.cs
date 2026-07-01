/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using System.Diagnostics;

namespace Integration.DevKit.Core;

/// <summary>
/// Utility methods for navigating, filtering, and transforming complex 
/// Dictionary and List structures, specifically targeting <see cref="Dictionary{TKey, TValue}"/> 
/// and <see cref="List{T}"/> where the values are loosely typed as <see cref="object"/>.
/// </summary>
public static class DictionaryUtils
{
    /// <summary>
    /// Retrieves a <see cref="Dictionary{string, object}"/> from the source using a key or dot-notation path.
    /// </summary>
    /// <param name="dictionary">The source dictionary.</param>
    /// <param name="keyPath">The key or dot-notation path to the dictionary.</param>
    /// <returns>A <see cref="Dictionary{string, object}"/> if found and of correct type; otherwise an empty <see cref="Dictionary{string, object}"/>.</returns>
    public static Dictionary<string, object> GetDictionary(Dictionary<string, object> dictionary, string keyPath)
    {
        return GetValue(dictionary, keyPath, new Dictionary<string, object>());
    }

    /// <summary>
    /// Retrieves a <see cref="List{T}"/> from the source using a key or dot-notation path.
    /// </summary>
    /// <typeparam name="T">The type of elements in the list.</typeparam>
    /// <param name="dictionary">The source dictionary.</param>Laura
    /// <param name="keyPath">The key or dot-notation path to the list.</param>
    /// <returns>A <see cref="List{T}"/> if found and of the correct type; otherwise an empty <see cref="List{T}"/>.</returns>
    public static List<T> GetList<T>(Dictionary<string, object> dictionary, string keyPath)
    {
        return GetValue(dictionary, keyPath, new List<T>());
    }

    /// <summary>
    /// Retrieves a <see cref="List{Dictionary{string, object}}"/> from the source using a key or dot-notation path.
    /// </summary>
    /// <param name="dictionary">The source dictionary.</param>Laura
    /// <param name="keyPath">The key or dot-notation path to the list.</param>
    /// <returns>A <see cref="List{Dictionary{string, object}}"/> if found and of correct type; otherwise an empty <see cref="List{Dictionary{string, object}}"/>.</returns>
    public static List<Dictionary<string, object>> GetDictionaryList(Dictionary<string, object> dictionary, string keyPath)
    {
        return GetValue(dictionary, keyPath, new List<Dictionary<string, object>>());
    }

    /// <summary>
    /// Navigates a dictionary by a specific key or a dot-notation path and attempts to convert the value to <typeparamref name="T"/>.
    /// </summary>
    /// <typeparam name="T">The target type for conversion.</typeparam>
    /// <param name="dictionary">The source dictionary.</param>
    /// <param name="keyPath">The key or dot-notation path (e.g., "Id" or "Meta.Metadata.Id").</param>
    /// <param name="defaultValue">Optional value to return if the key is missing, the value is null, or conversion fails.</param>
    /// <returns>The value converted to <typeparamref name="T"/>, or <paramref name="defaultValue"/>.</returns>
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
}
