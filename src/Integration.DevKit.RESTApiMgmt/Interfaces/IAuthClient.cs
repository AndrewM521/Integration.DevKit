/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using System.Text;
using Integration.DevKit.Core;

namespace Integration.DevKit.RESTApiMgmt.Interfaces;

/// <summary>
/// A minimal client handed to an <see cref="IAuthStrategy"/> so it can call an authentication endpoint
/// (e.g. an OAuth2 token endpoint) without re-entering the strategy itself.
/// </summary>
/// <remarks>
/// Requests made through this client never apply an <see cref="IAuthStrategy"/>, do not take a
/// concurrency slot from the owning <see cref="ApiClient"/>, and are not counted in its metrics.
/// <para>
/// Use a relative endpoint (e.g. <c>"/oauth/token"</c>) for an authentication endpoint on the same host as
/// the owning client's <c>BaseUrl</c>. Use an absolute URL (e.g. <c>"https://login.example.com/oauth/token"</c>)
/// for a different host; the client's default headers are not sent to a different host.
/// </para>
/// </remarks>
public interface IAuthClient
{
    /// <summary>
    /// Sends an asynchronous GET request without applying an auth strategy.
    /// </summary>
    /// <param name="endpointUrl">A relative (same host) or absolute (any host) URL.</param>
    /// <param name="httpContent">Optional <see cref="HttpContent"/> request content sent to the server.</param>
    /// <param name="requestHeaders">Optional dictionary of HTTP headers to include in the request.</param>
    /// <returns>An <see cref="ApiOperationResult{T}"/> containing the response body string.</returns>
    public Task<ApiOperationResult<string>> GetAsync(string endpointUrl, HttpContent? httpContent = null, Dictionary<string, string>? requestHeaders = null);

    /// <summary>
    /// Sends an asynchronous POST request without applying an auth strategy.
    /// </summary>
    /// <param name="endpointUrl">A relative (same host) or absolute (any host) URL.</param>
    /// <param name="httpContent">Optional <see cref="HttpContent"/> request content sent to the server.</param>
    /// <param name="requestHeaders">Optional dictionary of HTTP headers to include in the request.</param>
    /// <returns>An <see cref="ApiOperationResult{T}"/> containing the response body string.</returns>
    public Task<ApiOperationResult<string>> PostAsync(string endpointUrl, HttpContent? httpContent = null, Dictionary<string, string>? requestHeaders = null);

    /// <summary>
    /// Sends an asynchronous PUT request without applying an auth strategy.
    /// </summary>
    /// <param name="endpointUrl">A relative (same host) or absolute (any host) URL.</param>
    /// <param name="httpContent">The <see cref="HttpContent"/> request content sent to the server.</param>
    /// <param name="requestHeaders">Optional dictionary of HTTP headers to include in the request.</param>
    /// <returns>An <see cref="ApiOperationResult{T}"/> containing the response body string.</returns>
    public Task<ApiOperationResult<string>> PutAsync(string endpointUrl, HttpContent httpContent, Dictionary<string, string>? requestHeaders = null);

    /// <summary>
    /// Sends an asynchronous DELETE request without applying an auth strategy.
    /// </summary>
    /// <param name="endpointUrl">A relative (same host) or absolute (any host) URL.</param>
    /// <param name="httpContent">Optional <see cref="HttpContent"/> request content sent to the server.</param>
    /// <param name="requestHeaders">Optional dictionary of HTTP headers to include in the request.</param>
    /// <returns>An <see cref="ApiOperationResult{T}"/> containing the response body string.</returns>
    public Task<ApiOperationResult<string>> DeleteAsync(string endpointUrl, HttpContent? httpContent = null, Dictionary<string, string>? requestHeaders = null);

    /// <summary>
    /// Formats data into an <see cref="HttpContent"/> object suitable for request bodies
    /// (e.g. a <see cref="RESTApiMediaTypes.WWW_UrlEncoded"/> OAuth2 token request).
    /// </summary>
    /// <param name="mediaType">The intended <see cref="RESTApiMediaTypes"/>.</param>
    /// <param name="data">The raw string data to be converted.</param>
    /// <param name="encoding">The text <see cref="Encoding"/> to use. Defaults to UTF-8 if null.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the formatted <see cref="HttpContent"/>.</returns>
    public OperationResult<HttpContent> CreateHttpContent(RESTApiMediaTypes mediaType, string data, Encoding? encoding = null);
}
