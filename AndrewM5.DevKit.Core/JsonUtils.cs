using AndrewM5.DevKit.Core.Results;
using System.Text.Json;

namespace AndrewM5.DevKit.Core;

public static class JsonUtils
{
    public static OperationResult<List<Dictionary<string, object>>> ParseAndFilterJson(string rawJSON,
        IEnumerable<string>? FilterParentsListKey = null,
        string? FilterListKey = null, 
        IEnumerable<string>? FilterPropertyKeys = null)
    {
        var result = new OperationResult<List<Dictionary<string, object>>>();

        if (string.IsNullOrWhiteSpace(rawJSON))
        {
            return result.SetMethodSuccess(new List<Dictionary<string, object>>());
        }

        try
        {
            // 1. Parse ONLY the structure (low memory)
            using (var doc = JsonDocument.Parse(rawJSON))
            {
                // 2. Traverse the JsonElement tree
                JsonElement targetElement = doc.RootElement;

                // Navigate through Parents
                if (FilterParentsListKey != null)
                {
                    foreach (var key in FilterParentsListKey)
                    {
                        if (targetElement.ValueKind == JsonValueKind.Object && targetElement.TryGetProperty(key, out var nextElement))
                        {
                            targetElement = nextElement;
                        }
                        else
                        {
                            break;
                        }
                    }
                }

                // Navigate to the specific List Key (e.g., "results")
                if (!string.IsNullOrWhiteSpace(FilterListKey))
                {
                    if (targetElement.ValueKind == JsonValueKind.Object && targetElement.TryGetProperty(FilterListKey, out var listElement))
                    {
                        targetElement = listElement;
                    }
                }

                // 3. ONLY NOW convert the specific objects we need
                var resultList = new List<Dictionary<string, object>>();
                if (targetElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in targetElement.EnumerateArray())
                    {
                        var dict = ConvertToFilteredDictionary(item, FilterPropertyKeys);
                        if (dict != null)
                        {
                            resultList.Add(dict);
                        }
                    }
                }
                else if (targetElement.ValueKind == JsonValueKind.Object)
                {
                    var dict = ConvertToFilteredDictionary(targetElement, FilterPropertyKeys);
                    if (dict != null)
                    {
                        resultList.Add(dict);
                    }
                }

                return result.SetMethodSuccess(resultList);
            }
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    public static OperationResult<List<Dictionary<string, object>>> FilterNestedDictionaries(Dictionary<string, object> originalDictionary,
        IEnumerable<string>? FilterParentsListKey = null, string?
        FilterListKey = null,
        IEnumerable<string>? FilterPropertyKeys = null)
    {
        var result = new OperationResult<List<Dictionary<string, object>>>();

        var json = ParseObjectToJson(originalDictionary);
        if (!json.MethodSuccess)
        {
            return result.SetMethodFailure(json.Exception);
        }

        return ParseAndFilterJson(json.Result, FilterParentsListKey, FilterListKey, FilterPropertyKeys);
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

    public static OperationResult<Dictionary<string, object>> ParseJsonToDictionary(string json)
    {
        var result = new OperationResult<Dictionary<string, object>>();

        try
        {
            using (var doc = JsonDocument.Parse(json))
            {
                object? parsedElement = ConvertJsonElementToNativeObject(doc.RootElement);

                if (parsedElement == null)
                {
                    throw new Exception("Parsed element is null.");
                }

                return result.SetMethodSuccess((Dictionary<string, object>)parsedElement);
            }
        }
        catch (Exception ex) 
        { 
            return result.SetMethodFailure(ex);
        }
    }
    
    public static OperationResult<string> ParseObjectToJson(object obj)
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

    private static Dictionary<string, object>? ConvertToFilteredDictionary(JsonElement element, IEnumerable<string>? propertyKeys)
    {
        if (element.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var dict = new Dictionary<string, object>();
        IEnumerable<string>? keys = null;

        if (propertyKeys?.Any() == true)
        {
            keys = propertyKeys;
        }

        foreach (var prop in element.EnumerateObject())
        {
            // Only convert if the user asked for it, or if they didn't provide a filter
            if (keys == null || keys.Contains(prop.Name))
            {
                dict[prop.Name] = ConvertJsonElementToNativeObject(prop.Value)!;
            }
        }

        return dict;
    }
}
