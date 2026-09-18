/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using System.Net;
using System.Text;
using Integration.DevKit.Core;
using Integration.DevKit.RESTApiMgmt.Interfaces;

namespace Integration.DevKit.RESTApiMgmt;

/// <summary>
/// The <see cref="IAuthClient"/> an <see cref="ApiClient"/> hands to its <see cref="IAuthStrategy"/>.
/// Sends requests without an auth strategy and outside the owning client's rate limiter and metrics.
/// </summary>
internal sealed class AuthClient : IAuthClient
{
    private readonly HttpClient _httpClient;
    private readonly Lazy<HttpClient>? _headerFreeClient;

    /// <param name="httpClient">The owning client's <see cref="HttpClient"/>, used for relative and same-host requests.</param>
    /// <param name="headerFreeClientFactory">
    /// Creates an <see cref="HttpClient"/> with no default headers, used for absolute URLs on a different host so
    /// the owning client's default headers (API keys, tenant ids, ...) are not sent there. When
    /// <see langword="null"/>, every request goes through <paramref name="httpClient"/>.
    /// </param>
    internal AuthClient(HttpClient httpClient, Func<HttpClient>? headerFreeClientFactory = null)
    {
        _httpClient = httpClient;
        // PublicationOnly so a failed creation is retried on the next call instead of being cached.
        _headerFreeClient = headerFreeClientFactory == null
            ? null
            : new Lazy<HttpClient>(headerFreeClientFactory, LazyThreadSafetyMode.PublicationOnly);
    }

    public Task<ApiOperationResult<string>> GetAsync(string endpointUrl, HttpContent? httpContent = null, Dictionary<string, string>? requestHeaders = null)
    {
        return SendAsync(endpointUrl, client => ApiRequest.GetAsync(client, endpointUrl, httpContent, requestHeaders));
    }

    public Task<ApiOperationResult<string>> PostAsync(string endpointUrl, HttpContent? httpContent = null, Dictionary<string, string>? requestHeaders = null)
    {
        return SendAsync(endpointUrl, client => ApiRequest.PostAsync(client, endpointUrl, httpContent, requestHeaders));
    }

    public Task<ApiOperationResult<string>> PutAsync(string endpointUrl, HttpContent httpContent, Dictionary<string, string>? requestHeaders = null)
    {
        return SendAsync(endpointUrl, client => ApiRequest.PutAsync(client, endpointUrl, httpContent, requestHeaders));
    }

    public Task<ApiOperationResult<string>> DeleteAsync(string endpointUrl, HttpContent? httpContent = null, Dictionary<string, string>? requestHeaders = null)
    {
        return SendAsync(endpointUrl, client => ApiRequest.DeleteAsync(client, endpointUrl, httpContent, requestHeaders));
    }

    public OperationResult<HttpContent> CreateHttpContent(RESTApiMediaTypes mediaType, string data, Encoding? encoding = null)
    {
        return ApiClient.BuildHttpContent(mediaType, data, encoding);
    }

    private Task<ApiOperationResult<string>> SendAsync(string endpointUrl, Func<HttpClient, Task<ApiOperationResult<string>>> send)
    {
        HttpClient client;

        try
        {
            client = SelectClient(endpointUrl);
        }
        catch (Exception ex)
        {
            var failure = new ApiOperationResult<string>()
                .SetApiFailure(HttpStatusCode.InternalServerError, ex, null, "Unable to create an HTTP client for the auth request");

            return Task.FromResult(failure);
        }

        return send(client);
    }

    /// <summary>
    /// Picks the header-free client when <paramref name="endpointUrl"/> is an absolute http(s) URL whose scheme,
    /// host, and port differ from the main client's <see cref="HttpClient.BaseAddress"/>; otherwise the main client.
    /// </summary>
    private HttpClient SelectClient(string endpointUrl)
    {
        if (_headerFreeClient != null
            && _httpClient.BaseAddress != null
            && Uri.TryCreate(endpointUrl, UriKind.Absolute, out var target)
            && (target.Scheme == Uri.UriSchemeHttp || target.Scheme == Uri.UriSchemeHttps)
            && !string.Equals(
                target.GetLeftPart(UriPartial.Authority),
                _httpClient.BaseAddress.GetLeftPart(UriPartial.Authority),
                StringComparison.OrdinalIgnoreCase))
        {
            return _headerFreeClient.Value;
        }

        return _httpClient;
    }
}
