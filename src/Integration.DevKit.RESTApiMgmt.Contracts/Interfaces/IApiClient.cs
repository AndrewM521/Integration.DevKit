/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.Core;
using Integration.DevKit.CredentialMgmt.Contracts;
using System.Text;

namespace Integration.DevKit.RESTApiMgmt.Contracts;

/// <summary>
/// Defines a contract for a specialized HTTP client capable of performing RESTful operations,
/// managing credentials, and tracking client-side metrics.
/// </summary>
public interface IApiClient : IAsyncDisposable
{
    /// <summary>
    /// Gets the current runtime configuration settings for the API client.
    /// </summary>
    public ApiClientSettings RuntimeSettings { get; }

    /// <summary>
    /// Gets the metrics tracking object for monitoring client performance and usage.
    /// </summary>
    public IApiClientMetrics ClientMetrics { get; }

    /// <summary>
    /// Gets the unique identifier or display name for this client instance.
    /// </summary>
    public string ClientName { get; }

    /// <summary>
    /// Re-derives everything this client caches from <see cref="RuntimeSettings"/> (the underlying
    /// <see cref="HttpClient"/>'s base address, timeout, and default headers, plus the concurrent-request
    /// rate limiter). Call this after mutating <see cref="RuntimeSettings"/> in place so the change takes effect.
    /// </summary>
    /// <returns>A <see cref="NullOperationResult"/> indicating success or failure.</returns>
    public NullOperationResult Initialize();

    #region Asynchronous HTTP Methods
    /// <summary>
    /// Sends an asynchronous GET request to the specified endpoint.
    /// </summary>
    /// <param name="endpointUrl">The relative or absolute URL of the resource.</param>
    /// <param name="httpContent">Optional <see cref="HttpContent"/> request content sent to the server.</param>
    /// <param name="requestHeaders">Optional dictionary of HTTP headers to include in the request.</param>
    /// <returns>A task representing the result as an <see cref="ApiOperationResult{T}"/> containing the response body string.</returns>
    public Task<ApiOperationResult<string>> GetAsync(string endpointUrl, HttpContent? httpContent = null, Dictionary<string, string>? requestHeaders = null);

    /// <summary>
    /// Sends an asynchronous POST request with an optional specified body to the specified endpoint.
    /// </summary>
    /// <param name="endpointUrl">The relative or absolute URL of the resource.</param>
    /// <param name="httpContent">Optional <see cref="HttpContent"/> request content sent to the server.</param>
    /// <param name="requestHeaders">Optional dictionary of HTTP headers to include in the request.</param>
    /// <returns>A task representing the result as an <see cref="ApiOperationResult{T}"/> containing the response body string.</returns>
    public Task<ApiOperationResult<string>> PostAsync(string endpointUrl, HttpContent? httpContent = null, Dictionary<string, string>? requestHeaders = null);

    /// <summary>
    /// Sends an asynchronous PUT request to the specified endpoint.
    /// </summary>
    /// <param name="endpointUrl">The relative or absolute URL of the resource.</param>
    /// <param name="httpContent">The <see cref="HttpContent"/> request content sent to the server.</param>
    /// <param name="requestHeaders">Optional dictionary of HTTP headers to include in the request.</param>
    /// <returns>A task representing the result as an <see cref="ApiOperationResult{T}"/> containing the response body string.</returns>
    public Task<ApiOperationResult<string>> PutAsync(string endpointUrl, HttpContent httpContent, Dictionary<string, string>? requestHeaders = null);

    /// <summary>
    /// Sends an asynchronous DELETE request to the specified endpoint.
    /// </summary>
    /// <param name="endpointUrl">The relative or absolute URL of the resource.</param>
    /// <param name="httpContent">Optional <see cref="HttpContent"/> request content sent to the server.</param>
    /// <param name="requestHeaders">Optional dictionary of HTTP headers to include in the request.</param>
    /// <returns>A task representing the result as an <see cref="ApiOperationResult{T}"/> containing the response body string.</returns>
    public Task<ApiOperationResult<string>> DeleteAsync(string endpointUrl, HttpContent? httpContent = null, Dictionary<string, string>? requestHeaders = null);
    #endregion

    #region Synchronous HTTP Methods
    /// <summary>
    /// Sends a synchronous GET request to the specified endpoint.
    /// </summary>
    /// <param name="endpointUrl">The relative or absolute URL of the resource.</param>
    /// <param name="httpContent">Optional <see cref="HttpContent"/> request content sent to the server.</param>
    /// <param name="requestHeaders">Optional dictionary of HTTP headers to include in the request.</param>
    /// <returns>An <see cref="ApiOperationResult{T}"/> containing the response body string.</returns>
    public ApiOperationResult<string> Get(string endpointUrl, HttpContent? httpContent = null, Dictionary<string, string>? requestHeaders = null);

    /// <summary>
    /// Sends a synchronous POST request with an optional specified body to the specified endpoint.
    /// </summary>
    /// <param name="endpointUrl">The relative or absolute URL of the resource.</param>
    /// <param name="httpContent">Optional <see cref="HttpContent"/> request content sent to the server.</param>
    /// <param name="requestHeaders">Optional dictionary of HTTP headers to include in the request.</param>
    /// <returns>An <see cref="ApiOperationResult{T}"/> containing the response body string.</returns>
    public ApiOperationResult<string> Post(string endpointUrl, HttpContent? httpContent = null, Dictionary<string, string>? requestHeaders = null);

    /// <summary>
    /// Sends a synchronous PUT request to the specified endpoint.
    /// </summary>
    /// <param name="endpointUrl">The relative or absolute URL of the resource.</param>
    /// <param name="httpContent">The <see cref="HttpContent"/> request content sent to the server.</param>
    /// <param name="requestHeaders">Optional dictionary of HTTP headers to include in the request.</param>
    /// <returns>An <see cref="ApiOperationResult{T}"/> containing the response body string.</returns>
    public ApiOperationResult<string> Put(string endpointUrl, HttpContent httpContent, Dictionary<string, string>? requestHeaders = null);

    /// <summary>
    /// Sends a synchronous DELETE request to the specified endpoint.
    /// </summary>
    /// <param name="endpointUrl">The relative or absolute URL of the resource.</param>
    /// <param name="httpContent">Optional <see cref="HttpContent"/> request content sent to the server.</param>
    /// <param name="requestHeaders">Optional dictionary of HTTP headers to include in the request.</param>
    /// <returns>An <see cref="ApiOperationResult{T}"/> containing the response body string.</returns>
    public ApiOperationResult<string> Delete(string endpointUrl, HttpContent? httpContent = null, Dictionary<string, string>? requestHeaders = null);
    #endregion

    #region Credentials
    /// <summary>
    /// Injects an <see cref="ISecretStore"/> implementation to manage sensitive credential storage.
    /// </summary>
    /// <param name="secretStore">The secret store to be used by this client.</param>
    public void SetSecretStore(ISecretStore secretStore);

    /// <summary>
    /// Sets the credentials for the client in the current secret store.
    /// </summary>
    /// <param name="username">The username for authentication.</param>
    /// <param name="password">The password for authentication.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating success or failure.</returns>
    public NullOperationResult SetCredentials(string username, string password);

    /// <summary>
    /// Retrieves the stored username from the credential store.
    /// </summary>
    /// <returns>A <see cref="OperationResult{T}"/> containing the retrieved username.</returns>
    public OperationResult<string> GetUsername();

    /// <summary>
    /// Retrieves the stored password from the credential store.
    /// </summary>
    /// <returns>A <see cref="OperationResult{T}"/> containing the retrieved password.</returns>
    public OperationResult<string> GetPassword();

    /// <summary>
    /// Removes a specific credential based on the provided key.
    /// </summary>
    /// <param name="key">The key identifying the credential to delete.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating completion status.</returns>
    public NullOperationResult DeleteCredential(string key);

    /// <summary>
    /// Clears all credentials associated with this client from the secret store.
    /// </summary>
    /// <returns>A <see cref="NullOperationResult"/> indicating completion status.</returns>
    public NullOperationResult DeleteAllCredentials();

    /// <summary>
    /// Injects an <see cref="IAuthStrategy"/> that is applied to every outgoing request
    /// (e.g. an OAuth2 bearer token), independently of any Basic-style credentials.
    /// </summary>
    /// <param name="authStrategy">The strategy to apply, or <see langword="null"/> to stop applying one.</param>
    public void SetAuthStrategy(IAuthStrategy? authStrategy);
    #endregion

    #region Helper Methods
    /// <summary>
    /// Formats data into an <see cref="HttpContent"/> object suitable for request bodies.
    /// </summary>
    /// <param name="mediaType">The intended <see cref="RESTApiMediaTypes"/>.</param>
    /// <param name="data">The raw string data to be converted.</param>
    /// <param name="encoding">The text <see cref="Encoding"/> to use. Defaults to UTF-8 if null.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the formatted <see cref="HttpContent"/>.</returns>
    public OperationResult<HttpContent> CreateHttpContent(RESTApiMediaTypes mediaType, string data, Encoding? encoding = null);

    /// <summary>
    /// Adds a default header to the client that will be applied to all subsequent requests.
    /// </summary>
    /// <param name="key">The header name.</param>
    /// <param name="value">The header value.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating success.</returns>
    public NullOperationResult AddDefaultHeader(string key, string value);
    #endregion

    /// <summary>
    /// Logging method to output current <see cref="ApiClientSettings"/> to the logs.
    /// </summary>
    /// <param name="calledFromManager">Indicates if the call originated from a management orchestrator.</param>
    public void LogRuntimeSettings(bool calledFromManager = false);
}

/// <summary>
/// Specifies the media types (MIME types) used for REST API requests and responses.
/// </summary>
public enum RESTApiMediaTypes
{
    /// <summary>
    /// Represents "application/json". The standard format for modern RESTful APIs.
    /// </summary>
    Json,

    /// <summary>
    /// Represents "application/xml". Used for legacy systems or SOAP-based services.
    /// </summary>
    Xml,

    /// <summary>
    /// Represents "text/plain". Used for simple, unformatted text data.
    /// </summary>
    PlainText,

    /// <summary>
    /// Represents "application/x-www-form-urlencoded". 
    /// Commonly used for simple form submissions and OAuth2 token requests.
    /// </summary>
    WWW_UrlEncoded
}