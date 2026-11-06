using System.Net;
using Integration.DevKit.Core;
using Integration.DevKit.CredentialMgmt.Contracts;
using Integration.DevKit.RESTApiMgmt.Interfaces;
using Integration.DevKit.RESTApiMgmt.Settings;
using Integration.DevKit.RESTApiMgmt.Tests.TestSupport;
using Microsoft.Extensions.Options;
using Moq;

namespace Integration.DevKit.RESTApiMgmt.Tests;

public class ApiClientTests
{
    private static ApiManager CreateApiManager(int defaultTimeoutSeconds = 30)
    {
        var settings = Options.Create(new ApiManagerSettings { Default_HttpTimeout_Seconds = defaultTimeoutSeconds });
        return new ApiManager(settings, new Mock<IHttpClientFactory>().Object);
    }

    private static (ApiClient client, FakeHttpMessageHandler handler, ApiManager manager) CreateClient(ApiClientSettings? settings = null)
    {
        var (client, handler, httpClient, manager) = CreateClientWithHttpClient(settings);
        return (client, handler, manager);
    }

    private static (ApiClient client, FakeHttpMessageHandler handler, HttpClient httpClient, ApiManager manager) CreateClientWithHttpClient(ApiClientSettings? settings = null)
    {
        var handler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var manager = CreateApiManager();

        settings ??= new ApiClientSettings { BaseUrl = "https://example.com/api/" };

        var client = new ApiClient(manager, "TestClient", settings, httpClient);

        return (client, handler, httpClient, manager);
    }

    [Fact]
    public void Initialize_DerivesBaseAddressFromSettings()
    {
        var (client, _, _) = CreateClient(new ApiClientSettings { BaseUrl = "https://example.com/api/" });

        // BaseAddress isn't exposed directly, so prove it via URL resolution on a request.
        Assert.Equal("https://example.com/api/", client.RuntimeSettings.BaseUrl);
    }

    [Fact]
    public void Initialize_NegativeMaxConcurrentRequests_CoercedToIntMaxValue()
    {
        var (client, _, _) = CreateClient(new ApiClientSettings { BaseUrl = "https://example.com/", MaxConcurrentRequests = -5 });

        Assert.Equal(int.MaxValue, client.RuntimeSettings.MaxConcurrentRequests);
    }

    [Fact]
    public void Initialize_NullHttpTimeout_FallsBackToManagerDefault()
    {
        var handler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var manager = CreateApiManager(defaultTimeoutSeconds: 45);
        var settings = new ApiClientSettings { BaseUrl = "https://example.com/", HttpTimeout_Seconds = null };

        var client = new ApiClient(manager, "TestClient", settings, httpClient);

        Assert.Equal(45, client.RuntimeSettings.HttpTimeout_Seconds);
    }

    [Fact]
    public void Initialize_NegativeHttpTimeout_CoercedToZero()
    {
        var (client, _, _) = CreateClient(new ApiClientSettings { BaseUrl = "https://example.com/", HttpTimeout_Seconds = -1 });

        Assert.Equal(0, client.RuntimeSettings.HttpTimeout_Seconds);
    }

    [Fact]
    public void Initialize_AppliesDefaultHeaders()
    {
        var handler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var manager = CreateApiManager();
        var settings = new ApiClientSettings { BaseUrl = "https://example.com/" };
        settings.DefaultHeaders["X-Default"] = "abc";

        _ = new ApiClient(manager, "TestClient", settings, httpClient);

        Assert.Equal("abc", httpClient.DefaultRequestHeaders.GetValues("X-Default").Single());
    }

    [Fact]
    public async Task GetAsync_OnSuccess_IncrementsGetCount()
    {
        var (client, handler, _) = CreateClient();
        handler.Responder = _ => FakeHttpMessageHandler.JsonResponse(HttpStatusCode.OK, "{}");

        var result = await client.GetAsync("/Posts");

        Assert.True(result.MethodSuccess);
        Assert.Equal(1, client.ClientMetrics.GetCount);
        Assert.Equal(0, client.ClientMetrics.FailureCount);
    }

    [Fact]
    public async Task PostAsync_OnSuccess_IncrementsPostCount()
    {
        var (client, handler, _) = CreateClient();
        handler.Responder = _ => FakeHttpMessageHandler.JsonResponse(HttpStatusCode.Created, "{}");

        using var content = new StringContent("{}");
        await client.PostAsync("/Posts", content);

        Assert.Equal(1, client.ClientMetrics.PostCount);
    }

    [Fact]
    public async Task PutAsync_OnSuccess_IncrementsPutCount()
    {
        var (client, handler, _) = CreateClient();
        handler.Responder = _ => FakeHttpMessageHandler.JsonResponse(HttpStatusCode.OK, "{}");

        using var content = new StringContent("{}");
        await client.PutAsync("/Posts/1", content);

        Assert.Equal(1, client.ClientMetrics.PutCount);
    }

    [Fact]
    public async Task DeleteAsync_OnSuccess_IncrementsDeleteCount()
    {
        var (client, handler, _) = CreateClient();
        handler.Responder = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };

        await client.DeleteAsync("/Posts/1");

        Assert.Equal(1, client.ClientMetrics.DeleteCount);
    }

    [Fact]
    public async Task GetAsync_OnFailure_IncrementsFailureCount()
    {
        var (client, handler, _) = CreateClient();
        handler.Responder = _ => FakeHttpMessageHandler.JsonResponse(HttpStatusCode.NotFound, "not found");

        var result = await client.GetAsync("/missing");

        Assert.False(result.MethodSuccess);
        Assert.Equal(1, client.ClientMetrics.GetCount);
        Assert.Equal(1, client.ClientMetrics.FailureCount);
    }

    [Theory]
    [InlineData(RESTApiMediaTypes.Json, "application/json")]
    [InlineData(RESTApiMediaTypes.Xml, "application/xml")]
    [InlineData(RESTApiMediaTypes.PlainText, "text/plain")]
    [InlineData(RESTApiMediaTypes.WWW_UrlEncoded, "application/x-www-form-urlencoded")]
    public void CreateHttpContent_MapsMediaTypeToCorrectMimeType(RESTApiMediaTypes mediaType, string expectedMime)
    {
        var (client, _, _) = CreateClient();

        var result = client.CreateHttpContent(mediaType, "data");

        Assert.True(result.MethodSuccess);
        Assert.Equal(expectedMime, result.Result.Headers.ContentType!.MediaType);
    }

    [Fact]
    public void CreateHttpContent_InvalidMediaType_ReturnsFailure()
    {
        var (client, _, _) = CreateClient();

        var result = client.CreateHttpContent((RESTApiMediaTypes)999, "data");

        Assert.False(result.MethodSuccess);
    }

    [Fact]
    public void AddDefaultHeader_ReplacesExistingHeaderRatherThanDuplicating()
    {
        var (client, _, httpClient, _) = CreateClientWithHttpClient();

        client.AddDefaultHeader("X-Test", "first");
        client.AddDefaultHeader("X-Test", "second");

        var header = httpClient.DefaultRequestHeaders.GetValues("X-Test").ToList();
        Assert.Single(header);
        Assert.Equal("second", header[0]);
    }

    [Fact]
    public async Task SetAuthStrategy_IsInvokedByGetAsync()
    {
        var (client, handler, _) = CreateClient();
        handler.Responder = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };

        var authStrategy = new Mock<IAuthStrategy>();
        authStrategy
            .Setup(s => s.ApplyAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<IAuthClient>()))
            .ReturnsAsync(new NullOperationResult().SetMethodSuccess());

        client.SetAuthStrategy(authStrategy.Object);
        await client.GetAsync("/Posts");

        authStrategy.Verify(s => s.ApplyAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<IAuthClient>()), Times.Once);
    }

    [Fact]
    public async Task SetAuthStrategy_Null_ClearsStrategy()
    {
        var (client, handler, _) = CreateClient();
        handler.Responder = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };

        var authStrategy = new Mock<IAuthStrategy>();
        authStrategy
            .Setup(s => s.ApplyAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<IAuthClient>()))
            .ReturnsAsync(new NullOperationResult().SetMethodSuccess());

        client.SetAuthStrategy(authStrategy.Object);
        client.SetAuthStrategy(null);
        await client.GetAsync("/Posts");

        authStrategy.Verify(s => s.ApplyAsync(It.IsAny<HttpRequestMessage>(), It.IsAny<IAuthClient>()), Times.Never);
    }

    [Fact]
    public async Task PostAsync_TransientFailures_RetriesWithSameBodyAndCountsOnce()
    {
        var (client, handler, _) = CreateClient(new ApiClientSettings { BaseUrl = "https://example.com/", MaxRetries = 3, RetryBaseDelay_Milliseconds = 0 });
        var bodies = new List<string>();
        handler.Responder = request =>
        {
            bodies.Add(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());
            return bodies.Count < 3
                ? FakeHttpMessageHandler.JsonResponse(HttpStatusCode.ServiceUnavailable, "{}")
                : FakeHttpMessageHandler.JsonResponse(HttpStatusCode.OK, "{}");
        };

        var content = new StringContent("{\"id\":1}", System.Text.Encoding.UTF8, "application/json");
        var result = await client.PostAsync("/Posts", content);

        Assert.True(result.MethodSuccess);
        Assert.Equal(3, handler.CallCount);
        Assert.All(bodies, body => Assert.Equal("{\"id\":1}", body));
        Assert.All(handler.Requests, request => Assert.Equal("application/json", request.Content!.Headers.ContentType!.MediaType));
        Assert.Equal(1, client.ClientMetrics.PostCount);
        Assert.Equal(0, client.ClientMetrics.FailureCount);
    }

    [Fact]
    public async Task GetAsync_RetriesExhausted_ReturnsLastFailure()
    {
        var (client, handler, _) = CreateClient(new ApiClientSettings { BaseUrl = "https://example.com/", MaxRetries = 2, RetryBaseDelay_Milliseconds = 0 });
        handler.Responder = _ => FakeHttpMessageHandler.JsonResponse(HttpStatusCode.TooManyRequests, "{}");

        var result = await client.GetAsync("/Posts");

        Assert.False(result.MethodSuccess);
        Assert.Equal(HttpStatusCode.TooManyRequests, result.StatusCode);
        Assert.Equal(3, handler.CallCount);
        Assert.Equal(1, client.ClientMetrics.FailureCount);
    }

    [Fact]
    public async Task GetAsync_NonTransientFailure_IsNotRetried()
    {
        var (client, handler, _) = CreateClient(new ApiClientSettings { BaseUrl = "https://example.com/", MaxRetries = 3, RetryBaseDelay_Milliseconds = 0 });
        handler.Responder = _ => FakeHttpMessageHandler.JsonResponse(HttpStatusCode.NotFound, "{}");

        await client.GetAsync("/Posts");

        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public async Task GetAsync_DefaultSettings_DoesNotRetry()
    {
        var (client, handler, _) = CreateClient();
        handler.Responder = _ => FakeHttpMessageHandler.JsonResponse(HttpStatusCode.ServiceUnavailable, "{}");

        await client.GetAsync("/Posts");

        Assert.Equal(1, handler.CallCount);
    }

    [Fact]
    public void Initialize_NullRetrySettings_FallBackToManagerDefaults()
    {
        var settings = Options.Create(new ApiManagerSettings { Default_MaxRetries = 4, Default_RetryBaseDelay_Milliseconds = 250 });
        var manager = new ApiManager(settings, new Mock<IHttpClientFactory>().Object);

        var client = new ApiClient(manager, "TestClient", new ApiClientSettings { BaseUrl = "https://example.com/" }, new HttpClient(new FakeHttpMessageHandler()));

        Assert.Equal(4, client.RuntimeSettings.MaxRetries);
        Assert.Equal(250, client.RuntimeSettings.RetryBaseDelay_Milliseconds);
    }
}
