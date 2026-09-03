/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.Core;
using Integration.DevKit.CredentialMgmt.Contracts;
using Integration.DevKit.RESTApiMgmt.Contracts;
using System.Net.Http.Headers;
using System.Text.Json;

namespace Integration.DevKit.RESTApiMgmt;

/// <summary>
/// An <see cref="IAuthStrategy"/> implementing the OAuth2 client-credentials grant (with
/// refresh-token support), suitable for backend, service-to-service <see cref="IApiClient"/> calls.
/// </summary>
/// <remarks>
/// The client secret is read from an <see cref="ISecretReader"/> on every refresh rather than being
/// captured once — this lets it be sourced from a <c>ConfigurationSecretReader</c>, a
/// <c>CompositeSecretReader</c>, or an encrypted <c>FileSecretStore</c>, without this class caring
/// which. If an <see cref="ISecretStore"/> is supplied for refresh-token persistence, any
/// <c>refresh_token</c> returned by the authorization server is saved so it survives process restarts.
/// </remarks>
public class OAuth2ClientCredentialsAuthStrategy : IAuthStrategy
{
    private const string ClientSecretKey = "client_secret";
    private const string RefreshTokenKey = "refresh_token";
    private static readonly TimeSpan ExpiryBuffer = TimeSpan.FromSeconds(60);

    private readonly HttpClient _tokenHttpClient;
    private readonly string _tokenEndpoint;
    private readonly string _clientId;
    private readonly string _credentialContainer;
    private readonly ISecretReader _secretReader;
    private readonly ISecretStore? _refreshTokenStore;
    private readonly string? _scope;

    private readonly SemaphoreSlim _refreshLock = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _expiresAtUtc = DateTimeOffset.MinValue;

    /// <summary>
    /// Initializes a new instance of the <see cref="OAuth2ClientCredentialsAuthStrategy"/> class.
    /// </summary>
    /// <param name="tokenHttpClient">The <see cref="HttpClient"/> used to call <paramref name="tokenEndpoint"/>.</param>
    /// <param name="tokenEndpoint">The absolute URL of the OAuth2 token endpoint.</param>
    /// <param name="clientId">The OAuth2 client identifier.</param>
    /// <param name="credentialContainer">The container/file name passed to <paramref name="secretReader"/> and <paramref name="refreshTokenStore"/>.</param>
    /// <param name="secretReader">The source the <c>client_secret</c> is read from under key <c>"client_secret"</c>, re-read on every refresh.</param>
    /// <param name="refreshTokenStore">Optional store used to persist/restore the <c>refresh_token</c> under key <c>"refresh_token"</c>.</param>
    /// <param name="scope">Optional space-delimited OAuth2 scope to request on the client-credentials grant.</param>
    public OAuth2ClientCredentialsAuthStrategy(
        HttpClient tokenHttpClient,
        string tokenEndpoint,
        string clientId,
        string credentialContainer,
        ISecretReader secretReader,
        ISecretStore? refreshTokenStore = null,
        string? scope = null)
    {
        _tokenHttpClient = tokenHttpClient ?? throw new ArgumentNullException(nameof(tokenHttpClient));
        _tokenEndpoint = tokenEndpoint ?? throw new ArgumentNullException(nameof(tokenEndpoint));
        _clientId = clientId ?? throw new ArgumentNullException(nameof(clientId));
        _credentialContainer = credentialContainer ?? throw new ArgumentNullException(nameof(credentialContainer));
        _secretReader = secretReader ?? throw new ArgumentNullException(nameof(secretReader));
        _refreshTokenStore = refreshTokenStore;
        _scope = scope;
    }

    /// <inheritdoc/>
    public async Task<NullOperationResult> ApplyAsync(HttpRequestMessage request)
    {
        var result = new NullOperationResult();

        try
        {
            if (_accessToken == null || DateTimeOffset.UtcNow >= _expiresAtUtc - ExpiryBuffer)
            {
                var refreshResult = await RefreshTokenAsync();
                if (!refreshResult.MethodSuccess)
                {
                    throw refreshResult.Exception;
                }
            }

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _accessToken);

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    private async Task<NullOperationResult> RefreshTokenAsync()
    {
        var result = new NullOperationResult();

        await _refreshLock.WaitAsync();

        try
        {
            // Re-check now that we hold the lock — another caller may have already refreshed.
            if (_accessToken != null && DateTimeOffset.UtcNow < _expiresAtUtc - ExpiryBuffer)
            {
                return result.SetMethodSuccess();
            }

            var getClientSecret = _secretReader.GetKey(_credentialContainer, ClientSecretKey);
            if (!getClientSecret.MethodSuccess)
            {
                throw getClientSecret.Exception;
            }

            string? storedRefreshToken = null;
            if (_refreshTokenStore != null)
            {
                var getRefreshToken = _refreshTokenStore.GetKey(_credentialContainer, RefreshTokenKey);
                if (getRefreshToken.MethodSuccess)
                {
                    storedRefreshToken = getRefreshToken.Result;
                }
            }

            var formParams = storedRefreshToken != null
                ? new Dictionary<string, string>
                {
                    ["grant_type"] = "refresh_token",
                    ["refresh_token"] = storedRefreshToken,
                    ["client_id"] = _clientId,
                    ["client_secret"] = getClientSecret.Result
                }
                : new Dictionary<string, string>
                {
                    ["grant_type"] = "client_credentials",
                    ["client_id"] = _clientId,
                    ["client_secret"] = getClientSecret.Result
                };

            if (!string.IsNullOrWhiteSpace(_scope))
            {
                formParams["scope"] = _scope;
            }

            using var content = new FormUrlEncodedContent(formParams);
            using var response = await _tokenHttpClient.PostAsync(_tokenEndpoint, content);

            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException($"Token endpoint returned {(int)response.StatusCode} {response.StatusCode}: {responseBody}");
            }

            using var doc = JsonDocument.Parse(responseBody);
            var root = doc.RootElement;

            if (!root.TryGetProperty("access_token", out var accessTokenElement))
            {
                throw new InvalidOperationException("Token response did not contain an 'access_token'.");
            }

            _accessToken = accessTokenElement.GetString();

            _expiresAtUtc = root.TryGetProperty("expires_in", out var expiresInElement)
                ? DateTimeOffset.UtcNow.AddSeconds(expiresInElement.GetInt32())
                : DateTimeOffset.UtcNow.AddMinutes(5);

            if (_refreshTokenStore != null && root.TryGetProperty(RefreshTokenKey, out var refreshTokenElement))
            {
                var newRefreshToken = refreshTokenElement.GetString();
                if (!string.IsNullOrEmpty(newRefreshToken))
                {
                    _refreshTokenStore.SetKey(_credentialContainer, RefreshTokenKey, newRefreshToken);
                }
            }

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
        finally
        {
            _refreshLock.Release();
        }
    }
}
