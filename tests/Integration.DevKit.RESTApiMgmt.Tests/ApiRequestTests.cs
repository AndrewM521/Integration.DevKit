using System.Net;
using Integration.DevKit.Core;
using Integration.DevKit.RESTApiMgmt.Interfaces;
using Integration.DevKit.RESTApiMgmt.Tests.TestSupport;
using Moq;

namespace Integration.DevKit.RESTApiMgmt.Tests;

public class ApiRequestTests
{
    private static (HttpClient client, FakeHttpMessageHandler handler) CreateClient(string? baseUrl = null)
    {
        var handler = new FakeHttpMessageHandler();
        var client = new HttpClient(handler);

        if (baseUrl != null)
        {
            client.BaseAddress = new Uri(baseUrl);
        }

        return (client, handler);
    }

    [Fact]
    public async Task GetAsync_OnSuccess_ReturnsSuccessResultWithBody()
    {
        var (client, handler) = CreateClient("https://example.com/api/");
        handler.Responder = _ => FakeHttpMessageHandler.JsonResponse(HttpStatusCode.OK, "{\"id\":1}");

        var result = await ApiRequest.GetAsync(client, "/Posts/1");

        Assert.True(result.MethodSuccess);
        Assert.Equal("{\"id\":1}", result.Result);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task PostAsync_OnSuccess_ReturnsSuccessResult()
    {
        var (client, handler) = CreateClient("https://example.com/api/");
        handler.Responder = _ => FakeHttpMessageHandler.JsonResponse(HttpStatusCode.Created, "{\"id\":2}");

        using var content = new StringContent("{\"title\":\"foo\"}");
        var result = await ApiRequest.PostAsync(client, "/Posts", content);

        Assert.True(result.MethodSuccess);
        Assert.Equal(HttpStatusCode.Created, result.StatusCode);
    }

    [Fact]
    public async Task PutAsync_OnSuccess_ReturnsSuccessResult()
    {
        var (client, handler) = CreateClient("https://example.com/api/");
        handler.Responder = _ => FakeHttpMessageHandler.JsonResponse(HttpStatusCode.OK, "{\"id\":1}");

        using var content = new StringContent("{\"title\":\"bar\"}");
        var result = await ApiRequest.PutAsync(client, "/Posts/1", content);

        Assert.True(result.MethodSuccess);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task DeleteAsync_OnSuccess_ReturnsSuccessResult()
    {
        var (client, handler) = CreateClient("https://example.com/api/");
        handler.Responder = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };

        var result = await ApiRequest.DeleteAsync(client, "/Posts/1");

        Assert.True(result.MethodSuccess);
        Assert.Equal(HttpStatusCode.OK, result.StatusCode);
    }

    [Fact]
    public async Task GetAsync_OnNotFound_ReturnsFailureWithResponseBodyAndException()
    {
        var (client, handler) = CreateClient("https://example.com/api/");
        handler.Responder = _ => FakeHttpMessageHandler.JsonResponse(HttpStatusCode.NotFound, "route not found");

        var result = await ApiRequest.GetAsync(client, "/this-route-does-not-exist");

        Assert.False(result.MethodSuccess);
        Assert.Equal(HttpStatusCode.NotFound, result.StatusCode);
        Assert.Equal("route not found", result.ResponseBody);
        Assert.NotNull(result.Exception);
    }

    [Fact]
    public async Task GetAsync_OnNetworkFailure_ReturnsServiceUnavailableFailure()
    {
        var (client, handler) = CreateClient("https://example.com/api/");
        handler.Responder = _ => throw new HttpRequestException("connection refused");

        var result = await ApiRequest.GetAsync(client, "/Posts");

        Assert.False(result.MethodSuccess);
        Assert.Equal(HttpStatusCode.ServiceUnavailable, result.StatusCode);
    }

    [Theory]
    [InlineData("https://example.com/api", "/Posts", "https://example.com/api/Posts")]
    [InlineData("https://example.com/api/", "/Posts", "https://example.com/api/Posts")]
    [InlineData("https://example.com/api/", "Posts", "https://example.com/api/Posts")]
    public async Task GetAsync_WithBaseAddress_ResolvesUrlCorrectly(string baseUrl, string endpoint, string expectedUrl)
    {
        var (client, handler) = CreateClient(baseUrl);
        handler.Responder = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };

        var result = await ApiRequest.GetAsync(client, endpoint);

        Assert.Equal(expectedUrl, result.RequestUrl);
    }

    [Fact]
    public async Task GetAsync_WithoutBaseAddress_UsesEndpointAsAbsoluteUrl()
    {
        var (client, handler) = CreateClient(baseUrl: null);
        handler.Responder = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };

        var result = await ApiRequest.GetAsync(client, "https://example.com/direct");

        Assert.Equal("https://example.com/direct", result.RequestUrl);
    }

    [Fact]
    public async Task GetAsync_WithContentTypeHeaderButNoContent_HeaderFailureIsSwallowedAndRequestStillSucceeds()
    {
        // AddHeadersToRequest catches its own per-header failures into an OperationResult that
        // ApiRequest never inspects, so a bad header (Content-Type with no HttpContent to attach
        // it to) is silently dropped rather than surfacing to the caller.
        var (client, handler) = CreateClient("https://example.com/api/");
        handler.Responder = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };

        var headers = new Dictionary<string, string> { ["Content-Type"] = "application/json" };

        var result = await ApiRequest.GetAsync(client, "/Posts", content: null, headers: headers);

        Assert.True(result.MethodSuccess);
        Assert.Single(handler.Requests);
    }

    [Theory]
    [InlineData("basic dXNlcjpwYXNz", "basic", "dXNlcjpwYXNz")]
    [InlineData("bearer abc123", "bearer", "abc123")]
    [InlineData("Digest abc123", "Digest", "abc123")]
    [InlineData("justatoken", "Bearer", "justatoken")]
    public async Task GetAsync_AuthorizationHeader_IsParsedIntoSchemeAndParameter(string headerValue, string expectedScheme, string expectedParameter)
    {
        var (client, handler) = CreateClient("https://example.com/api/");
        handler.Responder = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };

        var headers = new Dictionary<string, string> { ["Authorization"] = headerValue };
        await ApiRequest.GetAsync(client, "/Posts", headers: headers);

        var sentRequest = Assert.Single(handler.Requests);
        Assert.Equal(expectedScheme, sentRequest.Headers.Authorization!.Scheme);
        Assert.Equal(expectedParameter, sentRequest.Headers.Authorization!.Parameter);
    }

    [Fact]
    public async Task GetAsync_UnknownHeader_IsAddedAsIs()
    {
        var (client, handler) = CreateClient("https://example.com/api/");
        handler.Responder = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };

        var headers = new Dictionary<string, string> { ["X-Custom"] = "value1" };
        await ApiRequest.GetAsync(client, "/Posts", headers: headers);

        var sentRequest = Assert.Single(handler.Requests);
        Assert.Equal("value1", sentRequest.Headers.GetValues("X-Custom").Single());
    }

    [Fact]
    public async Task GetAsync_MultipleBadHeaders_FailuresAreSwallowedAndRequestStillSucceeds()
    {
        // Same as above: AddHeadersToRequest aggregates per-header failures internally but its
        // result is never checked by ApiRequest, so even multiple bad headers don't throw.
        var (client, handler) = CreateClient("https://example.com/api/");
        handler.Responder = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };

        var headers = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/json", // no content on a GET -> fails internally
            ["User-Agent"] = "not a valid product token!!" // ParseAdd fails internally too
        };

        var result = await ApiRequest.GetAsync(client, "/Posts", headers: headers);

        Assert.True(result.MethodSuccess);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task GetAsync_WhenAuthStrategyFails_ShortCircuitsWithUnauthorizedAndNeverCallsHandler()
    {
        var (client, handler) = CreateClient("https://example.com/api/");
        handler.Responder = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };

        var authStrategy = new Mock<IAuthStrategy>();
        authStrategy
            .Setup(s => s.ApplyAsync(It.IsAny<HttpRequestMessage>()))
            .ReturnsAsync(new NullOperationResult().SetMethodFailure(new InvalidOperationException("token refresh failed")));

        var result = await ApiRequest.GetAsync(client, "/Posts", authStrategy: authStrategy.Object);

        Assert.False(result.MethodSuccess);
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task GetAsync_WhenAuthStrategySucceeds_RequestIsSent()
    {
        var (client, handler) = CreateClient("https://example.com/api/");
        handler.Responder = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };

        var authStrategy = new Mock<IAuthStrategy>();
        authStrategy
            .Setup(s => s.ApplyAsync(It.IsAny<HttpRequestMessage>()))
            .ReturnsAsync(new NullOperationResult().SetMethodSuccess());

        var result = await ApiRequest.GetAsync(client, "/Posts", authStrategy: authStrategy.Object);

        Assert.True(result.MethodSuccess);
        Assert.Single(handler.Requests);
    }
}
