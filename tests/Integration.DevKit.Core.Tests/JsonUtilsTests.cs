namespace Integration.DevKit.Core.Tests;

public class JsonUtilsTests
{
    private const string SampleJson = """
    {
        "id": 1,
        "name": "Widget",
        "active": true,
        "price": 12.5,
        "notes": null,
        "data": {
            "activities": [
                { "id": 1, "name": "Run" },
                { "id": 2, "name": "Swim" }
            ],
            "owner": {
                "id": 99,
                "name": "Alice"
            }
        }
    }
    """;

    [Fact]
    public void SerializeObjectToJson_Succeeds()
    {
        var result = JsonUtils.SerializeObjectToJson(new { A = 1, B = "x" });

        Assert.True(result.MethodSuccess);
        Assert.Contains("\"A\": 1", result.Result);
    }

    [Fact]
    public void DeserializeJsonToObject_ParsesObjectIntoDictionary()
    {
        var result = JsonUtils.DeserializeJsonToObject("""{"a":1,"b":"x"}""");

        Assert.True(result.MethodSuccess);
        var dict = Assert.IsType<Dictionary<string, object?>>(result.Result);
        Assert.Equal(1, dict["a"]);
        Assert.Equal("x", dict["b"]);
    }

    [Fact]
    public void DeserializeJsonToObject_InvalidJson_Fails()
    {
        var result = JsonUtils.DeserializeJsonToObject("not json");

        Assert.False(result.MethodSuccess);
    }

    [Fact]
    public void GetDictionary_SinglePath_ReturnsTargetObject()
    {
        var result = JsonUtils.GetDictionary(SampleJson, "data.owner");

        Assert.True(result.MethodSuccess);
        Assert.Equal(99, result.Result["id"]);
        Assert.Equal("Alice", result.Result["name"]);
    }

    [Fact]
    public void GetDictionary_MissingPath_ReturnsEmptyDictionary()
    {
        var result = JsonUtils.GetDictionary(SampleJson, "data.missing.nested");

        Assert.True(result.MethodSuccess);
        Assert.Empty(result.Result);
    }

    [Fact]
    public void GetDictionary_RootLevelPrimitive_WrapsUnderLastSegmentKey()
    {
        var result = JsonUtils.GetDictionary(SampleJson, "name");

        Assert.True(result.MethodSuccess);
        Assert.Equal("Widget", result.Result["name"]);
    }

    [Fact]
    public void GetDictionary_MultiplePaths_MergesUnderCommonParent()
    {
        var result = JsonUtils.GetDictionary(SampleJson, new List<string> { "data.owner.id", "data.owner.name" });

        Assert.True(result.MethodSuccess);
        Assert.Equal(99, result.Result["id"]);
        Assert.Equal("Alice", result.Result["name"]);
    }

    [Fact]
    public void GetDictionary_RemoveNullsTrue_StripsNullFields()
    {
        var result = JsonUtils.GetDictionary(SampleJson, keyPaths: null, removeNulls: true);

        Assert.True(result.MethodSuccess);
        Assert.False(result.Result.ContainsKey("notes"));
    }

    [Fact]
    public void GetDictionary_ArrayPath_RemoveNullsAppliesToTheWrappedItems()
    {
        // The array branch used to keep null items and nested nulls even with removeNulls on
        string json = "{\"items\":[{\"a\":1},null,[2,null]]}";

        var removed = JsonUtils.GetDictionary(json, "items", removeNulls: true);
        var kept = JsonUtils.GetDictionary(json, "items", removeNulls: false);

        var removedItems = Assert.IsType<List<object>>(removed.Result["items"]);
        Assert.Equal(2, removedItems.Count);
        Assert.Single(Assert.IsType<List<object>>(removedItems[1]));
        Assert.Equal(3, Assert.IsType<List<object>>(kept.Result["items"]).Count);
    }

    [Fact]
    public void GetDictionary_RemoveNullsFalse_PreservesNullFields()
    {
        var result = JsonUtils.GetDictionary(SampleJson, keyPaths: null, removeNulls: false);

        Assert.True(result.MethodSuccess);
        Assert.True(result.Result.ContainsKey("notes"));
        Assert.Null(result.Result["notes"]);
    }

    [Fact]
    public void GetList_WildcardProjection_ProjectsFieldAcrossArrayItems()
    {
        var result = JsonUtils.GetList<string>(SampleJson, "data.activities.name");

        Assert.True(result.MethodSuccess);
        Assert.Equal(new[] { "Run", "Swim" }, result.Result);
    }

    [Fact]
    public void GetList_ExplicitArrayIndex_ReturnsSingleItem()
    {
        var result = JsonUtils.GetDictionary(SampleJson, "data.activities.0");

        Assert.True(result.MethodSuccess);
        Assert.Equal(1, result.Result["id"]);
        Assert.Equal("Run", result.Result["name"]);
    }

    [Fact]
    public void GetDictionaryList_ArrayPath_ReturnsListOfDictionaries()
    {
        var result = JsonUtils.GetDictionaryList(SampleJson, "data.activities");

        Assert.True(result.MethodSuccess);
        Assert.Equal(2, result.Result.Count);
        Assert.Equal(1, result.Result[0]["id"]);
        Assert.Equal(2, result.Result[1]["id"]);
    }

    [Fact]
    public void ParseAndFilterJson_EmptyJson_Throws()
    {
        var result = JsonUtils.ParseAndFilterJson<Dictionary<string, object>>("");

        Assert.False(result.MethodSuccess);
    }

    [Fact]
    public void GetDictionary_KeyContainingDot_IsNormalizedAndNotTreatedAsPath()
    {
        const string json = """{"a.b": "literal-key-value"}""";

        var result = JsonUtils.GetDictionary(json, keyPaths: null);

        Assert.True(result.MethodSuccess);
        Assert.Equal("literal-key-value", result.Result["a_b"]);
    }

    [Fact]
    public void GetDictionary_PreserveRootLayout_ReturnsFullAncestralTree()
    {
        var result = JsonUtils.GetDictionary(SampleJson, "data.owner.name", Integration.DevKit.Core.JsonExtractionLayout.PreserveRoot);

        Assert.True(result.MethodSuccess);
        var data = Assert.IsType<Dictionary<string, object>>(result.Result["data"]);
        var owner = Assert.IsType<Dictionary<string, object>>(data["owner"]);
        Assert.Equal("Alice", owner["name"]);
    }

    [Fact]
    public void GetList_NullableItemType_KeepsNullsAndConvertsValues()
    {
        // Convert.ChangeType can't target int?, so this used to fail for every nullable list
        var list = JsonUtils.GetList<int?>("{\"ids\":[1,null,3]}", "ids", removeNulls: false);

        Assert.True(list.MethodSuccess);
        Assert.Equal(new int?[] { 1, null, 3 }, list.Result);
    }

    [Fact]
    public void DeserializeJsonToObject_PreciseNumber_KeepsEveryDigit()
    {
        // As a double this became 12345678901234.568
        var parsed = JsonUtils.DeserializeJsonToObject("{\"amount\":12345678901234.5678}");

        var dict = Assert.IsType<Dictionary<string, object?>>(parsed.Result);
        Assert.Equal(12345678901234.5678m, dict["amount"]);
    }

    [Fact]
    public void DeserializeJsonToObject_TinyExponentNumber_IsNotRoundedToZero()
    {
        // As a decimal this became 0
        var parsed = JsonUtils.DeserializeJsonToObject("{\"tiny\":1.23e-30,\"plain\":0.1,\"preciseExponent\":1.2345678901234567e5}");

        var dict = Assert.IsType<Dictionary<string, object?>>(parsed.Result);
        Assert.Equal(1.23e-30, dict["tiny"]);
        Assert.Equal(0.1m, dict["plain"]);
        // An exponent alone doesn't force double: decimal keeps all 17 digits here
        Assert.Equal(123456.78901234567m, dict["preciseExponent"]);
    }

    [Fact]
    public void ParseAndFilterJson_NoKeysAndPrimitiveRoot_WrapsUnderValueKey()
    {
        // keys![0] used to throw NullReferenceException here
        var result = JsonUtils.ParseAndFilterJson<Dictionary<string, object>>("42");

        Assert.True(result.MethodSuccess);
        Assert.Equal(42, result.Result["value"]);
    }

    [Fact]
    public void ParseAndFilterJson_MissingPathForReferenceType_SaysPathNotFound()
    {
        var result = JsonUtils.ParseAndFilterJson<string>("{\"a\":1}", new List<string> { "missing" });

        Assert.False(result.MethodSuccess);
        Assert.IsType<KeyNotFoundException>(result.Exception);
    }

    [Fact]
    public void ParseAndFilterJson_DottedKeyNextToUnderscoredKey_KeepsBothValues()
    {
        // Renaming "a.b" to "a_b" used to overwrite the existing "a_b"
        var result = JsonUtils.ParseAndFilterJson<Dictionary<string, object>>("{\"a.b\":1,\"a_b\":2,\"c.d\":3}");

        Assert.True(result.MethodSuccess);
        Assert.Equal(2, result.Result["a_b"]);
        Assert.Equal(1, result.Result["a.b"]);
        Assert.Equal(3, result.Result["c_d"]);
    }

    [Fact]
    public void TrailingComma_IsRejectedWhetherOrNotKeysHaveDots()
    {
        // Only JSON with a dotted key went through the lenient parser, so the same mistake passed or failed by key name
        Assert.False(JsonUtils.GetDictionary("{\"a\":1,}").MethodSuccess);
        Assert.False(JsonUtils.GetDictionary("{\"a.b\":1,}").MethodSuccess);
    }
}
