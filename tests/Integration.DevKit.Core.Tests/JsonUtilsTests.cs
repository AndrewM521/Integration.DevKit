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
}
