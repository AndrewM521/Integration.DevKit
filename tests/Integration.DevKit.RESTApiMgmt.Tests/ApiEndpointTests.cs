using System.Globalization;

namespace Integration.DevKit.RESTApiMgmt.Tests;

public class ApiEndpointTests
{
    [Fact]
    public void BuildSlashUrl_ValueWithSpace_EncodesSpaceAsPercent20()
    {
        // WebUtility.UrlEncode gave "New+York", which a server reads as a literal plus in a path
        var url = new ApiEndpoint("/weather").BuildSlashUrl(new Dictionary<string, object> { ["city"] = "New York" });

        Assert.Equal("/weather/city/New%20York", url.Result);
    }

    [Fact]
    public void BuildPositionalUrl_ValueWithSpace_EncodesSpaceAsPercent20()
    {
        var url = new ApiEndpoint("/users").BuildPositionalUrl(new List<object> { "a b" });

        Assert.Equal("/users/a%20b", url.Result);
    }

    [Fact]
    public void BuildQueryUrl_OnCommaDecimalCulture_FormatsNumbersAndDatesInvariantly()
    {
        var original = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = new CultureInfo("de-DE");
        try
        {
            var url = new ApiEndpoint("/items").BuildQueryUrl(new Dictionary<string, object>
            {
                ["price"] = 1.5,
                ["since"] = new DateTime(2026, 11, 16, 10, 0, 0, DateTimeKind.Utc)
            });

            Assert.Equal("/items?price=1.5&since=2026-11-16T10%3A00%3A00.0000000Z", url.Result);
        }
        finally
        {
            CultureInfo.CurrentCulture = original;
        }
    }

    [Fact]
    public void BuildQueryUrl_RouteWithAQuery_AddsToIt()
    {
        var url = new ApiEndpoint("search?type=all").BuildQueryUrl(new Dictionary<string, object> { ["q"] = "x" });

        Assert.Equal("search?type=all&q=x", url.Result);
    }

    [Fact]
    public async Task BuildQueryUrl_EncodesPerRfc3986_SpaceAsPercent20()
    {
        var query = new Dictionary<string, object> { ["name"] = "John Doe", ["tag"] = "a&b=c", ["skip"] = null! };

        var url = new ApiEndpoint("users/search").BuildQueryUrl(query);
        var asyncUrl = await new ApiEndpoint("users/search").BuildQueryUrlAsync(query);

        // Spaces as %20 (never "+"), reserved characters escaped, null values left out
        Assert.Equal("users/search?name=John%20Doe&tag=a%26b%3Dc", url.Result);
        Assert.Equal(url.Result, asyncUrl.Result);
    }
}
