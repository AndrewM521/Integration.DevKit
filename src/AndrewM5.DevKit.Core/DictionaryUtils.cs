using AndrewM5.DevKit.Core.Results;

namespace AndrewM5.DevKit.Core;

public static class DictionaryUtils
{
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
    
    public static OperationResult<Dictionary<string, object>> GetFirstDictionary(List<Dictionary<string, object>> dictionaries)
    {
        var result = new OperationResult<Dictionary<string, object>>();

        if (dictionaries != null && dictionaries.Count > 0 && dictionaries[0] != null)
        {
            return result.SetMethodSuccess(dictionaries[0]);
        }

        return result.SetMethodSuccess(new Dictionary<string, object>());
    }
    
    public static OperationResult<KeyValuePair<string, object>> FirstKeyValuePair(List<Dictionary<string, object>> dictionaries)
    {
        var result = new OperationResult<KeyValuePair<string, object>>();

        if (dictionaries != null && dictionaries.Count > 0 && dictionaries[0] != null)
        {
            var firstDict = dictionaries[0];

            if (firstDict.Count > 0)
            {
                return result.SetMethodSuccess(firstDict.First());
            }
        }

        return result.SetMethodSuccess(default);
    }

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

    public static Dictionary<string, object>? GetDictionary(Dictionary<string, object> dict, string key)
    {
        if (!dict.TryGetValue(key, out var value))
        {
            return null;
        }

        if (value is not Dictionary<string, object> nested)
        {
            return null;
        }

        return nested;
    }

    public static List<Dictionary<string, object>>? GetListDictionary(Dictionary<string, object> dict, string key)
    {
        if (!dict.TryGetValue(key, out var value))
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

    public static List<object>? GetList(Dictionary<string, object> dict, string key)
    {
        if (!dict.TryGetValue(key, out var value))
        {
            return null;
        }

        if (value is not List<object> list)
        {
            return null;
        }

        return list;
    }

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
                TraverseInternal(item, path, depth, ref matches);
            }
        }
    }
}
