using AndrewM5.DevKit.Core.Results;

namespace AndrewM5.DevKit.Core;

public static class DictionaryExtension
{
    public static OperationResult<List<object>> TraverseByPath(object curObject, string[] path, int depth)
    {
        var result = new OperationResult<List<object>>();
        var matches = new List<object>();

        try
        {
            TraverseInternal(curObject, path, depth, ref matches);

            return result.SetMethodSuccess(matches);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
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

    public static OperationResult<List<Dictionary<string, object>>> ExtractSubsetByKeys(List<Dictionary<string, object>> source, IEnumerable<string> keys)
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
}
