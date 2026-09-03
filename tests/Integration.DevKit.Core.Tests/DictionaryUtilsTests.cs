namespace Integration.DevKit.Core.Tests;

public class DictionaryUtilsTests
{
    private static Dictionary<string, object> BuildSource() => new()
    {
        ["Id"] = 5,
        ["Meta"] = new Dictionary<string, object>
        {
            ["Metadata"] = new Dictionary<string, object>
            {
                ["Name"] = "widget",
                ["Tags"] = new List<object> { "a", "b", "c" }
            }
        },
        ["Items"] = new List<object>
        {
            new Dictionary<string, object> { ["X"] = 1 },
            new Dictionary<string, object> { ["Y"] = 2 }
        }
    };

    [Fact]
    public void GetValue_SimpleKey_ReturnsConvertedValue()
    {
        var dict = BuildSource();

        var value = DictionaryUtils.GetValue<int>(dict, "Id");

        Assert.Equal(5, value);
    }

    [Fact]
    public void GetValue_DotPath_NavigatesNestedDictionaries()
    {
        var dict = BuildSource();

        var value = DictionaryUtils.GetValue<string>(dict, "Meta.Metadata.Name");

        Assert.Equal("widget", value);
    }

    [Fact]
    public void GetValue_MissingKey_ReturnsDefaultValue()
    {
        var dict = BuildSource();

        var value = DictionaryUtils.GetValue(dict, "Meta.Missing.Path", "fallback");

        Assert.Equal("fallback", value);
    }

    [Fact]
    public void GetValue_ConversionFailure_ReturnsDefaultValue()
    {
        var dict = BuildSource();

        var value = DictionaryUtils.GetValue(dict, "Meta.Metadata.Name", 42);

        Assert.Equal(42, value);
    }

    [Fact]
    public void GetValue_NullOrEmptyDictionary_ReturnsDefault()
    {
        var value = DictionaryUtils.GetValue<string>(null!, "Id", "fallback");

        Assert.Equal("fallback", value);
    }

    [Fact]
    public void GetValue_ListConversion_ConvertsEachItem()
    {
        var dict = BuildSource();

        var value = DictionaryUtils.GetValue<List<string>>(dict, "Meta.Metadata.Tags");

        Assert.Equal(new List<string> { "a", "b", "c" }, value);
    }

    [Fact]
    public void GetDictionary_ReturnsNestedDictionary()
    {
        var dict = BuildSource();

        var meta = DictionaryUtils.GetDictionary(dict, "Meta.Metadata");

        Assert.Equal("widget", meta["Name"]);
    }

    [Fact]
    public void GetDictionary_MissingPath_ReturnsEmptyDictionary()
    {
        var dict = BuildSource();

        var meta = DictionaryUtils.GetDictionary(dict, "Nope.Missing");

        Assert.Empty(meta);
    }

    [Fact]
    public void GetDictionaryList_ReturnsListOfDictionaries()
    {
        var dict = BuildSource();

        var items = DictionaryUtils.GetDictionaryList(dict, "Items");

        Assert.Equal(2, items.Count);
        Assert.Equal(1, items[0]["X"]);
    }

    [Fact]
    public void GetFirstDictionary_ReturnsFirstElement()
    {
        var list = new List<Dictionary<string, object>>
        {
            new() { ["A"] = 1 },
            new() { ["B"] = 2 }
        };

        var result = DictionaryUtils.GetFirstDictionary(list);

        Assert.True(result.MethodSuccess);
        Assert.Equal(1, result.Result["A"]);
    }

    [Fact]
    public void GetFirstDictionary_EmptyList_ReturnsEmptyDictionary()
    {
        var result = DictionaryUtils.GetFirstDictionary(new List<Dictionary<string, object>>());

        Assert.True(result.MethodSuccess);
        Assert.Empty(result.Result);
    }

    [Fact]
    public void FlattenListByKey_ListOfDictionaries_MergesAllKeys()
    {
        var source = new Dictionary<string, object>
        {
            ["Rows"] = new List<object>
            {
                new Dictionary<string, object> { ["A"] = 1 },
                new Dictionary<string, object> { ["B"] = 2 }
            }
        };

        var result = DictionaryUtils.FlattenListByKey(source, "Rows");

        Assert.True(result.MethodSuccess);
        Assert.Equal(1, result.Result["A"]);
        Assert.Equal(2, result.Result["B"]);
    }

    [Fact]
    public void FlattenListByKey_DuplicateKeys_LastOccurrenceWins()
    {
        var source = new Dictionary<string, object>
        {
            ["Rows"] = new List<object>
            {
                new Dictionary<string, object> { ["A"] = 1 },
                new Dictionary<string, object> { ["A"] = 2 }
            }
        };

        var result = DictionaryUtils.FlattenListByKey(source, "Rows");

        Assert.Equal(2, result.Result["A"]);
    }

    [Fact]
    public void FlattenListByKey_SingleNestedDictionary_MergesDirectly()
    {
        var source = new Dictionary<string, object>
        {
            ["Sub"] = new Dictionary<string, object> { ["A"] = 1 }
        };

        var result = DictionaryUtils.FlattenListByKey(source, "Sub");

        Assert.Equal(1, result.Result["A"]);
    }

    [Fact]
    public void FlattenListByKey_MissingKey_ReturnsEmptyDictionary()
    {
        var source = new Dictionary<string, object>();

        var result = DictionaryUtils.FlattenListByKey(source, "Missing");

        Assert.Empty(result.Result);
    }
}
