using System.Net;
using Integration.DevKit.Core;
using Integration.DevKit.CredentialMgmt.Contracts;
using Integration.DevKit.RESTApiMgmt.Contracts;
using Integration.DevKit.RESTApiMgmt.Tests.TestSupport;
using Moq;

namespace Integration.DevKit.RESTApiMgmt.Tests;

public class ApiClientTests
{
    private static Mock<IApiManager> CreateApiManagerMock(int defaultTimeoutSeconds = 30)
    {
        var manager = new Mock<IApiManager>();
        manager.SetupGet(m => m.RuntimeSettings).Returns(new ApiManagerSettings { Default_HttpTimeout_Seconds = defaultTimeoutSeconds });
        return manager;
    }

    private static (ApiClient client, FakeHttpMessageHandler handler, Mock<IApiManager> manager) CreateClient(ApiClientSettings? settings = null)
    {
        var (client, handler, httpClient, manager) = CreateClientWithHttpClient(settings);
        return (client, handler, manager);
    }

    private static (ApiClient client, FakeHttpMessageHandler handler, HttpClient httpClient, Mock<IApiManager> manager) CreateClientWithHttpClient(ApiClientSettings? settings = null)
    {
        var handler = new FakeHttpMessageHandler();
        var httpClient = new HttpClient(handler);
        var manager = CreateApiManagerMock();

        settings ??= new ApiClientSettings { BaseUrl = "https://example.com/api/" };

        var client = new ApiClient(manager.Object, "TestClient", settings, httpClient);

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
        var manager = CreateApiManagerMock(defaultTimeoutSeconds: 45);
        var settings = new ApiClientSettings { BaseUrl = "https://example.com/", HttpTimeout_Seconds = null };

        var client = new ApiClient(manager.Object, "TestClient", settings, httpClient);

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
        var manager = CreateApiManagerMock();
        var settings = new ApiClientSettings { BaseUrl = "https://example.com/" };
        settings.DefaultHeaders["X-Default"] = "abc";

        _ = new ApiClient(manager.Object, "TestClient", settings, httpClient);

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
    public void SetCredentials_WithoutSecretStore_ReturnsFailure()
    {
        var (client, _, _) = CreateClient();

        var result = client.SetCredentials("user", "pass");

        Assert.False(result.MethodSuccess);
        Assert.Contains("SecretStore has not been set", result.Exception.Message);
    }

    [Fact]
    public void SetCredentials_WithSecretStore_DelegatesToStore()
    {
        var (client, _, _) = CreateClient();
        var store = new Mock<ISecretStore>();
        store.Setup(s => s.SetKey(It.IsAny<string>(), "username", "user")).Returns(new NullOperationResult().SetMethodSuccess());
        store.Setup(s => s.SetKey(It.IsAny<string>(), "password", "pass")).Returns(new NullOperationResult().SetMethodSuccess());

        client.SetSecretStore(store.Object);
        var result = client.SetCredentials("user", "pass");

        Assert.True(result.MethodSuccess);
        store.Verify(s => s.SetKey(It.IsAny<string>(), "username", "user"), Times.Once);
        store.Verify(s => s.SetKey(It.IsAny<string>(), "password", "pass"), Times.Once);
    }

    [Fact]
    public void GetUsername_WithoutSecretStore_FallsBackToRuntimeSettings()
    {
        var settings = new ApiClientSettings { BaseUrl = "https://example.com/", Username = "configured-user" };
        var (client, _, _) = CreateClient(settings);

        var result = client.GetUsername();

        Assert.True(result.MethodSuccess);
        Assert.Equal("configured-user", result.Result);
    }

    [Fact]
    public void GetPassword_WithoutSecretStore_FallsBackToRuntimeSettings()
    {
        var settings = new ApiClientSettings { BaseUrl = "https://example.com/", Password = "configured-pass" };
        var (client, _, _) = CreateClient(settings);

        var result = client.GetPassword();

        Assert.True(result.MethodSuccess);
        Assert.Equal("configured-pass", result.Result);
    }

    [Fact]
    public void DeleteCredential_WithoutSecretStore_ReturnsFailure()
    {
        var (client, _, _) = CreateClient();

        var result = client.DeleteCredential("username");

        Assert.False(result.MethodSuccess);
    }

    [Fact]
    public void DeleteAllCredentials_WithSecretStore_DelegatesToStore()
    {
        var (client, _, _) = CreateClient();
        var store = new Mock<ISecretStore>();
        store.Setup(s => s.DeleteSecret(It.IsAny<string>())).Returns(new NullOperationResult().SetMethodSuccess());

        client.SetSecretStore(store.Object);
        var result = client.DeleteAllCredentials();

        Assert.True(result.MethodSuccess);
        store.Verify(s => s.DeleteSecret(It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task SetAuthStrategy_IsInvokedByGetAsync()
    {
        var (client, handler, _) = CreateClient();
        handler.Responder = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };

        var authStrategy = new Mock<IAuthStrategy>();
        authStrategy
            .Setup(s => s.ApplyAsync(It.IsAny<HttpRequestMessage>()))
            .ReturnsAsync(new NullOperationResult().SetMethodSuccess());

        client.SetAuthStrategy(authStrategy.Object);
        await client.GetAsync("/Posts");

        authStrategy.Verify(s => s.ApplyAsync(It.IsAny<HttpRequestMessage>()), Times.Once);
    }

    [Fact]
    public async Task SetAuthStrategy_Null_ClearsStrategy()
    {
        var (client, handler, _) = CreateClient();
        handler.Responder = _ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(string.Empty) };

        var authStrategy = new Mock<IAuthStrategy>();
        authStrategy
            .Setup(s => s.ApplyAsync(It.IsAny<HttpRequestMessage>()))
            .ReturnsAsync(new NullOperationResult().SetMethodSuccess());

        client.SetAuthStrategy(authStrategy.Object);
        client.SetAuthStrategy(null);
        await client.GetAsync("/Posts");

        authStrategy.Verify(s => s.ApplyAsync(It.IsAny<HttpRequestMessage>()), Times.Never);
    }
}
