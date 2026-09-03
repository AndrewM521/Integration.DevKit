using System.Net;
using System.Reflection;
using Integration.DevKit.Core;
using Integration.DevKit.CredentialMgmt.Contracts;
using Integration.DevKit.RESTApiMgmt.Tests.TestSupport;
using Moq;

namespace Integration.DevKit.RESTApiMgmt.Tests;

public class OAuth2ClientCredentialsAuthStrategyTests
{
    private const string TokenEndpoint = "https://auth.example.com/token";
    private const string ClientId = "test-client";
    private const string CredentialContainer = "OAuth";

    private static Mock<ISecretReader> CreateSecretReader(string clientSecret = "shh")
    {
        var reader = new Mock<ISecretReader>();
        reader
            .Setup(r => r.GetKey(CredentialContainer, "client_secret"))
            .Returns(new OperationResult<string>().SetMethodSuccess(clientSecret));
        return reader;
    }

    private static (HttpClient client, FakeHttpMessageHandler handler) CreateTokenClient()
    {
        var handler = new FakeHttpMessageHandler();
        return (new HttpClient(handler), handler);
    }

    private static void SetExpired(OAuth2ClientCredentialsAuthStrategy strategy)
    {
        var field = typeof(OAuth2ClientCredentialsAuthStrategy)
            .GetField("_expiresAtUtc", BindingFlags.NonPublic | BindingFlags.Instance)!;
        field.SetValue(strategy, DateTimeOffset.UtcNow.AddMinutes(-10));
    }

    /// <summary>
    /// Wraps a JSON response responder so the outgoing form-encoded request body is captured
    /// while its <see cref="HttpContent"/> is still alive (the strategy disposes it as soon as
    /// the token POST completes, before the awaiting test method resumes).
    /// </summary>
    private static Func<HttpRequestMessage, HttpResponseMessage> CaptureBodyThenRespond(List<string> capturedBodies, HttpStatusCode statusCode, string json)
    {
        return request =>
        {
            capturedBodies.Add(request.Content!.ReadAsStringAsync().GetAwaiter().GetResult());
            return FakeHttpMessageHandler.JsonResponse(statusCode, json);
        };
    }

    [Fact]
    public void Constructor_NullTokenHttpClient_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new OAuth2ClientCredentialsAuthStrategy(null!, TokenEndpoint, ClientId, CredentialContainer, CreateSecretReader().Object));
    }

    [Fact]
    public void Constructor_NullTokenEndpoint_Throws()
    {
        var (client, _) = CreateTokenClient();
        Assert.Throws<ArgumentNullException>(() =>
            new OAuth2ClientCredentialsAuthStrategy(client, null!, ClientId, CredentialContainer, CreateSecretReader().Object));
    }

    [Fact]
    public void Constructor_NullClientId_Throws()
    {
        var (client, _) = CreateTokenClient();
        Assert.Throws<ArgumentNullException>(() =>
            new OAuth2ClientCredentialsAuthStrategy(client, TokenEndpoint, null!, CredentialContainer, CreateSecretReader().Object));
    }

    [Fact]
    public void Constructor_NullCredentialContainer_Throws()
    {
        var (client, _) = CreateTokenClient();
        Assert.Throws<ArgumentNullException>(() =>
            new OAuth2ClientCredentialsAuthStrategy(client, TokenEndpoint, ClientId, null!, CreateSecretReader().Object));
    }

    [Fact]
    public void Constructor_NullSecretReader_Throws()
    {
        var (client, _) = CreateTokenClient();
        Assert.Throws<ArgumentNullException>(() =>
            new OAuth2ClientCredentialsAuthStrategy(client, TokenEndpoint, ClientId, CredentialContainer, null!));
    }

    [Fact]
    public async Task ApplyAsync_FirstCall_FetchesTokenAndSetsAuthorizationHeader()
    {
        var (tokenClient, handler) = CreateTokenClient();
        var capturedBodies = new List<string>();
        handler.Responder = CaptureBodyThenRespond(capturedBodies, HttpStatusCode.OK, "{\"access_token\":\"fake-token-1\",\"token_type\":\"Bearer\",\"expires_in\":3600}");

        var strategy = new OAuth2ClientCredentialsAuthStrategy(tokenClient, TokenEndpoint, ClientId, CredentialContainer, CreateSecretReader().Object);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/resource");

        var result = await strategy.ApplyAsync(request);

        Assert.True(result.MethodSuccess);
        Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
        Assert.Equal("fake-token-1", request.Headers.Authorization!.Parameter);
        Assert.Equal(1, handler.CallCount);

        var body = Assert.Single(capturedBodies);
        Assert.Contains("grant_type=client_credentials", body);
        Assert.Contains($"client_id={ClientId}", body);
    }

    [Fact]
    public async Task ApplyAsync_SecondImmediateCall_ReusesCachedToken()
    {
        var (tokenClient, handler) = CreateTokenClient();
        handler.Responder = _ => FakeHttpMessageHandler.JsonResponse(HttpStatusCode.OK, "{\"access_token\":\"fake-token-1\",\"expires_in\":3600}");

        var strategy = new OAuth2ClientCredentialsAuthStrategy(tokenClient, TokenEndpoint, ClientId, CredentialContainer, CreateSecretReader().Object);

        using var request1 = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/resource");
        await strategy.ApplyAsync(request1);

        using var request2 = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/resource");
        await strategy.ApplyAsync(request2);

        Assert.Equal(1, handler.CallCount);
        Assert.Equal("fake-token-1", request2.Headers.Authorization!.Parameter);
    }

    [Fact]
    public async Task ApplyAsync_AfterExpiry_RefreshesToken()
    {
        var (tokenClient, handler) = CreateTokenClient();
        var callCount = 0;
        handler.Responder = _ =>
        {
            callCount++;
            return FakeHttpMessageHandler.JsonResponse(HttpStatusCode.OK, $"{{\"access_token\":\"fake-token-{callCount}\",\"expires_in\":3600}}");
        };

        var strategy = new OAuth2ClientCredentialsAuthStrategy(tokenClient, TokenEndpoint, ClientId, CredentialContainer, CreateSecretReader().Object);

        using var request1 = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/resource");
        await strategy.ApplyAsync(request1);

        SetExpired(strategy);

        using var request2 = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/resource");
        await strategy.ApplyAsync(request2);

        Assert.Equal(2, handler.CallCount);
        Assert.Equal("fake-token-2", request2.Headers.Authorization!.Parameter);
    }

    [Fact]
    public async Task ApplyAsync_WhenClientSecretUnavailable_FailsWithoutCallingTokenEndpoint()
    {
        var (tokenClient, handler) = CreateTokenClient();
        handler.Responder = _ => FakeHttpMessageHandler.JsonResponse(HttpStatusCode.OK, "{\"access_token\":\"unused\"}");

        var reader = new Mock<ISecretReader>();
        reader
            .Setup(r => r.GetKey(CredentialContainer, "client_secret"))
            .Returns(new OperationResult<string>().SetMethodFailure(new InvalidOperationException("secret not found")));

        var strategy = new OAuth2ClientCredentialsAuthStrategy(tokenClient, TokenEndpoint, ClientId, CredentialContainer, reader.Object);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/resource");

        var result = await strategy.ApplyAsync(request);

        Assert.False(result.MethodSuccess);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task ApplyAsync_WhenTokenEndpointReturnsNonSuccess_Fails()
    {
        var (tokenClient, handler) = CreateTokenClient();
        handler.Responder = _ => FakeHttpMessageHandler.JsonResponse(HttpStatusCode.BadRequest, "{\"error\":\"invalid_client\"}");

        var strategy = new OAuth2ClientCredentialsAuthStrategy(tokenClient, TokenEndpoint, ClientId, CredentialContainer, CreateSecretReader().Object);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/resource");

        var result = await strategy.ApplyAsync(request);

        Assert.False(result.MethodSuccess);
        Assert.Contains("400", result.Exception.Message);
    }

    [Fact]
    public async Task ApplyAsync_WhenTokenResponseMissingAccessToken_Fails()
    {
        var (tokenClient, handler) = CreateTokenClient();
        handler.Responder = _ => FakeHttpMessageHandler.JsonResponse(HttpStatusCode.OK, "{\"token_type\":\"Bearer\"}");

        var strategy = new OAuth2ClientCredentialsAuthStrategy(tokenClient, TokenEndpoint, ClientId, CredentialContainer, CreateSecretReader().Object);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/resource");

        var result = await strategy.ApplyAsync(request);

        Assert.False(result.MethodSuccess);
        Assert.IsType<InvalidOperationException>(result.Exception);
    }

    [Fact]
    public async Task ApplyAsync_WithScope_IncludesScopeInTokenRequestBody()
    {
        var (tokenClient, handler) = CreateTokenClient();
        var capturedBodies = new List<string>();
        handler.Responder = CaptureBodyThenRespond(capturedBodies, HttpStatusCode.OK, "{\"access_token\":\"tok\",\"expires_in\":3600}");

        var strategy = new OAuth2ClientCredentialsAuthStrategy(tokenClient, TokenEndpoint, ClientId, CredentialContainer, CreateSecretReader().Object, scope: "read write");
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/resource");

        await strategy.ApplyAsync(request);

        var body = Assert.Single(capturedBodies);
        Assert.Contains("scope=read+write", body);
    }

    [Fact]
    public async Task ApplyAsync_WithoutScope_OmitsScopeFromTokenRequestBody()
    {
        var (tokenClient, handler) = CreateTokenClient();
        var capturedBodies = new List<string>();
        handler.Responder = CaptureBodyThenRespond(capturedBodies, HttpStatusCode.OK, "{\"access_token\":\"tok\",\"expires_in\":3600}");

        var strategy = new OAuth2ClientCredentialsAuthStrategy(tokenClient, TokenEndpoint, ClientId, CredentialContainer, CreateSecretReader().Object);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/resource");

        await strategy.ApplyAsync(request);

        var body = Assert.Single(capturedBodies);
        Assert.DoesNotContain("scope=", body);
    }

    [Fact]
    public async Task ApplyAsync_WithStoredRefreshToken_UsesRefreshTokenGrant()
    {
        var (tokenClient, handler) = CreateTokenClient();
        var capturedBodies = new List<string>();
        handler.Responder = CaptureBodyThenRespond(capturedBodies, HttpStatusCode.OK, "{\"access_token\":\"tok\",\"expires_in\":3600}");

        var refreshStore = new Mock<ISecretStore>();
        refreshStore
            .Setup(s => s.GetKey(CredentialContainer, "refresh_token"))
            .Returns(new OperationResult<string>().SetMethodSuccess("stored-refresh-token"));

        var strategy = new OAuth2ClientCredentialsAuthStrategy(tokenClient, TokenEndpoint, ClientId, CredentialContainer, CreateSecretReader().Object, refreshStore.Object);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/resource");

        await strategy.ApplyAsync(request);

        var body = Assert.Single(capturedBodies);
        Assert.Contains("grant_type=refresh_token", body);
        Assert.Contains("refresh_token=stored-refresh-token", body);
        Assert.DoesNotContain("grant_type=client_credentials", body);
    }

    [Fact]
    public async Task ApplyAsync_WhenServerReturnsNewRefreshToken_PersistsItToStore()
    {
        var (tokenClient, handler) = CreateTokenClient();
        handler.Responder = _ => FakeHttpMessageHandler.JsonResponse(HttpStatusCode.OK, "{\"access_token\":\"tok\",\"expires_in\":3600,\"refresh_token\":\"new-refresh-token\"}");

        var refreshStore = new Mock<ISecretStore>();
        refreshStore
            .Setup(s => s.GetKey(CredentialContainer, "refresh_token"))
            .Returns(new OperationResult<string>().SetMethodFailure(new InvalidOperationException("no stored refresh token")));
        refreshStore
            .Setup(s => s.SetKey(CredentialContainer, "refresh_token", "new-refresh-token"))
            .Returns(new NullOperationResult().SetMethodSuccess());

        var strategy = new OAuth2ClientCredentialsAuthStrategy(tokenClient, TokenEndpoint, ClientId, CredentialContainer, CreateSecretReader().Object, refreshStore.Object);
        using var request = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/resource");

        await strategy.ApplyAsync(request);

        refreshStore.Verify(s => s.SetKey(CredentialContainer, "refresh_token", "new-refresh-token"), Times.Once);
    }

    [Fact]
    public async Task ApplyAsync_ConcurrentCallsWhileExpired_OnlyRefreshesOnce()
    {
        var (tokenClient, handler) = CreateTokenClient();
        var callCount = 0;
        handler.Responder = _ =>
        {
            Interlocked.Increment(ref callCount);
            Thread.Sleep(50); // widen the window so both callers race into the refresh path
            return FakeHttpMessageHandler.JsonResponse(HttpStatusCode.OK, "{\"access_token\":\"tok\",\"expires_in\":3600}");
        };

        var strategy = new OAuth2ClientCredentialsAuthStrategy(tokenClient, TokenEndpoint, ClientId, CredentialContainer, CreateSecretReader().Object);

        using var request1 = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/resource");
        using var request2 = new HttpRequestMessage(HttpMethod.Get, "https://api.example.com/resource");

        await Task.WhenAll(strategy.ApplyAsync(request1), strategy.ApplyAsync(request2));

        Assert.Equal(1, callCount);
    }
}
