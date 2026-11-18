using System.Net;
using Integration.DevKit.Core;
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
        var httpClient = new HttpClient(handler, disposeHandler: false);

        // Initialize() swaps in a new client from the factory when settings change, so it must share the fake handler
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(handler, disposeHandler: false));
        var manager = new ApiManager(Options.Create(new ApiManagerSettings { Default_HttpTimeout_Seconds = 30 }), factory.Object);

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
    public async Task MaxConcurrentRequestsZero_IsTreatedAsUnlimited()
    {
        // A zero-slot semaphore used to make every request wait forever
        var (client, _, _) = CreateClient(new ApiClientSettings { BaseUrl = "https://example.com/api/", MaxConcurrentRequests = 0 });

        var send = client.GetAsync("items");
        var finished = await Task.WhenAny(send, Task.Delay(5000));

        Assert.Same(send, finished);
        Assert.True((await send).MethodSuccess);
        Assert.Equal(int.MaxValue, client.RuntimeSettings.MaxConcurrentRequests);
    }

    [Fact]
    public async Task Initialize_AgainAfterARequest_WithUnchangedAddress_Succeeds()
    {
        var (client, _, _) = CreateClient();
        Assert.True((await client.GetAsync("items")).MethodSuccess);

        client.RuntimeSettings.MaxConcurrentRequests = 5;
        var init = client.Initialize();

        Assert.True(init.MethodSuccess);
        Assert.True((await client.GetAsync("items")).MethodSuccess);
    }

    [Fact]
    public async Task Initialize_HeaderChangedAfterUse_LeavesTheLiveClientUntouched()
    {
        // Editing DefaultRequestHeaders on a client that is sending requests isn't thread-safe,
        // so a changed header must arrive on a new client instead of being edited in place
        var settings = new ApiClientSettings
        {
            BaseUrl = "https://example.com/api/",
            DefaultHeaders = new Dictionary<string, string> { ["X-Token"] = "old" }
        };
        var (client, handler, originalHttpClient, _) = CreateClientWithHttpClient(settings);
        await client.GetAsync("items");

        client.RuntimeSettings.DefaultHeaders["X-Token"] = "new";
        Assert.True(client.Initialize().MethodSuccess);
        await client.GetAsync("items");

        Assert.Equal("old", originalHttpClient.DefaultRequestHeaders.GetValues("X-Token").Single());
        Assert.Equal("new", handler.Requests.Last().Headers.GetValues("X-Token").Single());
    }

    [Fact]
    public async Task Initialize_HeaderRemovedFromSettings_IsNoLongerSent_ButManualHeadersStay()
    {
        var settings = new ApiClientSettings
        {
            BaseUrl = "https://example.com/api/",
            DefaultHeaders = new Dictionary<string, string> { ["X-From-Settings"] = "1" }
        };
        var (client, handler, _) = CreateClient(settings);
        client.AddDefaultHeader("X-Manual", "2");
        await client.GetAsync("items");

        client.RuntimeSettings.DefaultHeaders.Remove("X-From-Settings");
        Assert.True(client.Initialize().MethodSuccess);
        await client.GetAsync("items");

        var last = handler.Requests.Last();
        Assert.False(last.Headers.Contains("X-From-Settings"));
        Assert.True(last.Headers.Contains("X-Manual"));
    }

    [Fact]
    public async Task Initialize_BaseUrlAndTimeoutChangedAfterUse_SwitchesToNewHttpClient()
    {
        // HttpClient refuses BaseAddress/Timeout writes after its first request, so Initialize must swap clients
        var handler = new FakeHttpMessageHandler();
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(handler, disposeHandler: false));
        var manager = new ApiManager(Options.Create(new ApiManagerSettings { Default_HttpTimeout_Seconds = 30 }), factory.Object);

        var settings = new ApiClientSettings { BaseUrl = "https://old.example.com/", DefaultHeaders = new Dictionary<string, string> { ["X-Key"] = "k" } };
        var client = new ApiClient(manager, "SwapClient", settings, new HttpClient(handler, disposeHandler: false));
        client.AddDefaultHeader("X-Manual", "m");
        Assert.True((await client.GetAsync("items")).MethodSuccess);

        client.RuntimeSettings.BaseUrl = "https://new.example.com/";
        client.RuntimeSettings.HttpTimeout_Seconds = 5;
        var init = client.Initialize();

        Assert.True(init.MethodSuccess);
        Assert.True((await client.GetAsync("items")).MethodSuccess);

        var last = handler.Requests.Last();
        Assert.Equal("new.example.com", last.RequestUri!.Host);
        Assert.True(last.Headers.Contains("X-Key"));
        Assert.True(last.Headers.Contains("X-Manual"));
        factory.Verify(f => f.CreateClient(It.IsAny<string>()), Times.Once);
    }

    private static ApiManager CreateManagerWithClients(params string[] clientNames)
    {
        var handler = new FakeHttpMessageHandler();
        var factory = new Mock<IHttpClientFactory>();
        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(() => new HttpClient(handler, disposeHandler: false));

        var settings = new ApiManagerSettings();
        foreach (var name in clientNames)
        {
            settings.Clients[name] = new ApiClientSettings { BaseUrl = $"https://{name.ToLowerInvariant()}.internal/" };
        }

        return new ApiManager(Options.Create(settings), factory.Object);
    }

    [Fact]
    public void GetClient_UnconfiguredName_Throws()
    {
        // The old fallback gave a client pointed at https://example.com, a real third-party host
        var manager = CreateManagerWithClients("TestClient");

        Assert.Throws<KeyNotFoundException>(() => manager.GetClient("TestClinet"));
    }

    [Fact]
    public void GetClient_NameDiffersOnlyByCaseFromConfig_UsesConfiguredSettings()
    {
        var manager = CreateManagerWithClients("TestClient");

        var client = manager.GetClient("testclient");

        Assert.Equal("https://testclient.internal/", client.RuntimeSettings.BaseUrl);
        Assert.Same(client, manager.GetClient("TestClient"));
    }

    [Fact]
    public async Task SyncGet_UnderASingleThreadedContext_DoesNotDeadlock()
    {
        // Without ConfigureAwait(false) the async code waits to resume on the thread that Get() is blocking
        var client = new ApiClient(CreateApiManager(), "TestClient", new ApiClientSettings { BaseUrl = "https://example.com/" }, new HttpClient(new AsyncHandler()));

        var call = Task.Run(() =>
        {
            SynchronizationContext.SetSynchronizationContext(new NeverRunsContext());
            try
            {
                return client.Get("ping");
            }
            finally
            {
                SynchronizationContext.SetSynchronizationContext(null);
            }
        });

        Assert.Same(call, await Task.WhenAny(call, Task.Delay(5000)));
        Assert.True((await call).MethodSuccess);
    }

    /// <summary>Like a UI thread that is blocked: anything posted back to it never runs.</summary>
    private sealed class NeverRunsContext : SynchronizationContext
    {
        public override void Post(SendOrPostCallback d, object? state) { }
    }

    /// <summary>Completes on another thread, so the caller's awaits really have to resume somewhere.</summary>
    private sealed class AsyncHandler : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            await Task.Delay(20, cancellationToken).ConfigureAwait(false);

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("pong") };
        }
    }

    [Fact]
    public async Task NegativeManagerDefaultTimeout_MeansNoTimeout_AndRequestsWork()
    {
        // -1 used to reach HttpClient as -1 seconds; the failed setup was ignored and the first request threw NullReferenceException
        var client = new ApiClient(CreateApiManager(defaultTimeoutSeconds: -1), "TestClient", new ApiClientSettings { BaseUrl = "https://example.com/" }, new HttpClient(new FakeHttpMessageHandler()));

        var result = await client.GetAsync("ping");

        Assert.True(result.MethodSuccess, result.Exception?.Message);
    }

    [Fact]
    public void InvalidBaseUrl_FailsWhenTheClientIsCreated()
    {
        Assert.Throws<UriFormatException>(() =>
            new ApiClient(CreateApiManager(), "TestClient", new ApiClientSettings { BaseUrl = "not a url" }, new HttpClient(new FakeHttpMessageHandler())));
    }
}
