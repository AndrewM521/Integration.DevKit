using System.Net;
using System.Net.Http.Headers;
using Integration.DevKit.Core;
using Integration.DevKit.RESTApiMgmt.Interfaces;
using Integration.DevKit.RESTApiMgmt.Settings;
using Integration.DevKit.RESTApiMgmt.Tests.TestSupport;
using Microsoft.Extensions.Options;
using Moq;

namespace Integration.DevKit.RESTApiMgmt.Tests;

public class AuthClientTests
{
    private const string BaseUrl = "https://example.com/api/";
    private static readonly TimeSpan HangGuard = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Fetches a "token" through the supplied <see cref="IAuthClient"/> and sets it as the bearer token,
    /// the way an OAuth2 strategy would.
    /// </summary>
    private sealed class TokenStrategy : IAuthStrategy
    {
        private readonly string _tokenEndpoint;
        private int _applyCount;

        public int ApplyCount => _applyCount;

        public TokenStrategy(string tokenEndpoint)
        {
            _tokenEndpoint = tokenEndpoint;
        }

        public async Task<NullOperationResult> ApplyAsync(HttpRequestMessage request, IAuthClient authClient)
        {
            Interlocked.Increment(ref _applyCount);

            var tokenResult = await authClient.PostAsync(_tokenEndpoint);
            if (!tokenResult.MethodSuccess)
            {
                return new NullOperationResult().SetMethodFailure(new InvalidOperationException("Token request failed"));
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", tokenResult.Result);

            return new NullOperationResult().SetMethodSuccess();
        }
    }

    private static HttpResponseMessage Respond(HttpRequestMessage request)
    {
        return request.RequestUri!.AbsolutePath.EndsWith("/oauth/token")
            ? FakeHttpMessageHandler.JsonResponse(HttpStatusCode.OK, "tok123")
            : FakeHttpMessageHandler.JsonResponse(HttpStatusCode.OK, "{}");
    }

    private static ApiManager CreateApiManager(Func<HttpClient>? headerFreeClient = null)
    {
        var factory = new Mock<IHttpClientFactory>();
        if (headerFreeClient != null)
        {
            factory.Setup(f => f.CreateClient("ApiClient")).Returns(headerFreeClient);
        }

        var settings = Options.Create(new ApiManagerSettings { Default_HttpTimeout_Seconds = 30 });
        return new ApiManager(settings, factory.Object);
    }

    private static (ApiClient client, FakeHttpMessageHandler handler) CreateClient(
        ApiClientSettings? settings = null,
        Func<HttpClient>? headerFreeClient = null)
    {
        var handler = new FakeHttpMessageHandler { Responder = Respond };
        var httpClient = new HttpClient(handler);

        settings ??= new ApiClientSettings { BaseUrl = BaseUrl };

        var client = new ApiClient(CreateApiManager(headerFreeClient), "TestClient", settings, httpClient);

        return (client, handler);
    }

    [Fact]
    public async Task TokenCallThroughAuthClient_DoesNotReenterStrategy()
    {
        var (client, handler) = CreateClient();
        var strategy = new TokenStrategy("/oauth/token");
        client.SetAuthStrategy(strategy);

        var result = await client.GetAsync("/Posts").WaitAsync(HangGuard);

        Assert.True(result.MethodSuccess);
        Assert.Equal(1, strategy.ApplyCount);

        var requests = handler.Requests.ToList();
        Assert.Equal(2, requests.Count);
        Assert.Equal(HttpMethod.Post, requests[0].Method);
        Assert.EndsWith("/api/oauth/token", requests[0].RequestUri!.AbsolutePath);
        Assert.Equal("Bearer", requests[1].Headers.Authorization!.Scheme);
        Assert.Equal("tok123", requests[1].Headers.Authorization!.Parameter);
    }

    [Fact]
    public async Task TokenCallThroughAuthClient_WithSingleConcurrencySlot_DoesNotDeadlock()
    {
        var (client, _) = CreateClient(new ApiClientSettings { BaseUrl = BaseUrl, MaxConcurrentRequests = 1 });
        client.SetAuthStrategy(new TokenStrategy("/oauth/token"));

        var result = await client.GetAsync("/Posts").WaitAsync(HangGuard);

        Assert.True(result.MethodSuccess);
    }

    [Fact]
    public async Task TokenCallThroughAuthClient_IsNotCountedInClientMetrics()
    {
        var (client, _) = CreateClient();
        client.SetAuthStrategy(new TokenStrategy("/oauth/token"));

        await client.GetAsync("/Posts").WaitAsync(HangGuard);

        Assert.Equal(1, client.ClientMetrics.GetCount);
        Assert.Equal(0, client.ClientMetrics.PostCount);
    }

    [Fact]
    public async Task SameHost_RelativeEndpoint_UsesBaseUrlAndDefaultHeaders()
    {
        var settings = new ApiClientSettings { BaseUrl = BaseUrl };
        settings.DefaultHeaders["X-Default"] = "abc";
        var (client, handler) = CreateClient(settings);
        client.SetAuthStrategy(new TokenStrategy("/oauth/token"));

        await client.GetAsync("/Posts").WaitAsync(HangGuard);

        var tokenRequest = handler.Requests.First();
        Assert.Equal("https://example.com/api/oauth/token", tokenRequest.RequestUri!.ToString());
        Assert.Equal("abc", tokenRequest.Headers.GetValues("X-Default").Single());
    }

    [Fact]
    public async Task SameHost_AbsoluteEndpoint_UsesMainClientAndDefaultHeaders()
    {
        var settings = new ApiClientSettings { BaseUrl = BaseUrl };
        settings.DefaultHeaders["X-Default"] = "abc";
        var (client, handler) = CreateClient(settings);
        client.SetAuthStrategy(new TokenStrategy("https://EXAMPLE.com/api/oauth/token"));

        await client.GetAsync("/Posts").WaitAsync(HangGuard);

        var tokenRequest = handler.Requests.First();
        Assert.Equal(HttpMethod.Post, tokenRequest.Method);
        Assert.Equal("abc", tokenRequest.Headers.GetValues("X-Default").Single());
    }

    [Fact]
    public async Task DifferentHost_AbsoluteEndpoint_GoesToThatHostWithoutDefaultHeaders()
    {
        var idpHandler = new FakeHttpMessageHandler { Responder = Respond };
        var settings = new ApiClientSettings { BaseUrl = BaseUrl };
        settings.DefaultHeaders["X-Default"] = "abc";
        var (client, apiHandler) = CreateClient(settings, headerFreeClient: () => new HttpClient(idpHandler));
        var strategy = new TokenStrategy("https://login.example.org/oauth/token");
        client.SetAuthStrategy(strategy);

        var result = await client.GetAsync("/Posts").WaitAsync(HangGuard);

        Assert.True(result.MethodSuccess);
        Assert.Equal(1, strategy.ApplyCount);

        var tokenRequest = Assert.Single(idpHandler.Requests);
        Assert.Equal("https://login.example.org/oauth/token", tokenRequest.RequestUri!.ToString());
        Assert.False(tokenRequest.Headers.Contains("X-Default"));

        var apiRequest = Assert.Single(apiHandler.Requests);
        Assert.Equal("https://example.com/api/Posts", apiRequest.RequestUri!.ToString());
        Assert.Equal("tok123", apiRequest.Headers.Authorization!.Parameter);
        Assert.Equal("abc", apiRequest.Headers.GetValues("X-Default").Single());
    }

    [Fact]
    public async Task DifferentHost_WhenHeaderFreeClientCannotBeCreated_ReturnsFailureInsteadOfThrowing()
    {
        // The mocked factory returns null, so creating the header-free client fails.
        var (client, apiHandler) = CreateClient();
        client.SetAuthStrategy(new TokenStrategy("https://login.example.org/oauth/token"));

        var result = await client.GetAsync("/Posts").WaitAsync(HangGuard);

        Assert.False(result.MethodSuccess);
        Assert.Equal(HttpStatusCode.Unauthorized, result.StatusCode);
        Assert.Empty(apiHandler.Requests);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("POST")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task AuthStrategy_IsAppliedToEveryVerb_AndHeaderReachesTheHandler(string verb)
    {
        var (client, handler) = CreateClient();
        client.SetAuthStrategy(new TokenStrategy("/oauth/token"));

        using var content = new StringContent("{}");
        var result = verb switch
        {
            "GET" => await client.GetAsync("/Posts"),
            "POST" => await client.PostAsync("/Posts", content),
            "PUT" => await client.PutAsync("/Posts/1", content),
            _ => await client.DeleteAsync("/Posts/1"),
        };

        Assert.True(result.MethodSuccess);

        var apiRequest = handler.Requests.Last();
        Assert.Equal(new HttpMethod(verb), apiRequest.Method);
        Assert.Equal("tok123", apiRequest.Headers.Authorization!.Parameter);
    }

    [Fact]
    public async Task ApiRequest_WithStrategyButNoAuthClient_SuppliesOneOverTheSameHttpClient()
    {
        var handler = new FakeHttpMessageHandler { Responder = Respond };
        var httpClient = new HttpClient(handler) { BaseAddress = new Uri(BaseUrl) };

        IAuthClient? captured = null;
        var strategy = new Mock<IAuthStrategy>();
        strategy
            .Setup(s => s.ApplyAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<IAuthClient>()))
            .Callback<HttpRequestMessage, IAuthClient>((_, authClient) => captured = authClient)
            .ReturnsAsync(new NullOperationResult().SetMethodSuccess());

        await ApiRequest.GetAsync(httpClient, "/Posts", authStrategy: strategy.Object);

        Assert.NotNull(captured);

        var content = captured!.CreateHttpContent(RESTApiMediaTypes.WWW_UrlEncoded, "grant_type=client_credentials");
        Assert.True(content.MethodSuccess);
        Assert.Equal("application/x-www-form-urlencoded", content.Result.Headers.ContentType!.MediaType);

        var tokenResult = await captured.PostAsync("/oauth/token", content.Result);
        Assert.True(tokenResult.MethodSuccess);
        Assert.Equal("tok123", tokenResult.Result);

        strategy.Verify(s => s.ApplyAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<IAuthClient>()), Times.Once);
    }
}
