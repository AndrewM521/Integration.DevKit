using AndrewM5.DevKit.ApiClientManagement.Abstractions.Options;
using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.CredentialManagement.Contracts.Interfaces;
using System.Text;

namespace AndrewM5.DevKit.ApiClientManagement.Contracts.Interfaces;

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

    #region Asynchronous HTTP Methods
    /// <summary>
    /// Sends an asynchronous GET request to the specified endpoint.
    /// </summary>
    /// <param name="endpointUrl">The relative or absolute URL of the resource.</param>
    /// <param name="requestHeaders">Optional dictionary of HTTP headers to include in the request.</param>
    /// <returns>A task representing the result as an <see cref="ApiOperationResult{T}"/> containing the response body string.</returns>
    public Task<ApiOperationResult<string>> GetAsync(string endpointUrl, Dictionary<string, string>? requestHeaders = null);

    /// <summary>
    /// Sends an asynchronous POST request with no body to the specified endpoint.
    /// </summary>
    /// <param name="endpointUrl">The relative or absolute URL of the resource.</param>
    /// <param name="requestHeaders">Optional dictionary of HTTP headers to include in the request.</param>
    /// <returns>A task representing the result as an <see cref="ApiOperationResult{T}"/> containing the response body string.</returns>
    public Task<ApiOperationResult<string>> PostAsync(string endpointUrl, Dictionary<string, string>? requestHeaders = null);

    /// <summary>
    /// Sends an asynchronous POST request with a specified body to the specified endpoint.
    /// </summary>
    /// <param name="endpointUrl">The relative or absolute URL of the resource.</param>
    /// <param name="httpContent">The <see cref="HttpContent"/> request content sent to the server.</param>
    /// <param name="requestHeaders">Optional dictionary of HTTP headers to include in the request.</param>
    /// <returns>A task representing the result as an <see cref="ApiOperationResult{T}"/> containing the response body string.</returns>
    public Task<ApiOperationResult<string>> PostAsync(string endpointUrl, HttpContent httpContent, Dictionary<string, string>? requestHeaders = null);

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
    /// <param name="requestHeaders">Optional dictionary of HTTP headers to include in the request.</param>
    /// <returns>A task representing the result as an <see cref="ApiOperationResult{T}"/> containing the response body string.</returns>
    public Task<ApiOperationResult<string>> DeleteAsync(string endpointUrl, Dictionary<string, string>? requestHeaders = null);
    #endregion

    #region Synchronous HTTP Methods
    /// <summary>
    /// Sends a synchronous GET request.
    /// </summary>
    /// <param name="endpointUrl">The relative or absolute URL of the resource.</param>
    /// <param name="requestHeaders">Optional dictionary of HTTP headers to include in the request.</param>
    /// <returns>An <see cref="ApiOperationResult{T}"/> containing the response body string.</returns>
    public ApiOperationResult<string> Get(string endpointUrl, Dictionary<string, string>? requestHeaders = null);

    /// <summary>
    /// Sends a synchronous POST request with no body.
    /// </summary>
    /// <param name="endpointUrl">The relative or absolute URL of the resource.</param>
    /// <param name="requestHeaders">Optional dictionary of HTTP headers to include in the request.</param>
    /// <returns>An <see cref="ApiOperationResult{T}"/> containing the response body string.</returns>
    public ApiOperationResult<string> Post(string endpointUrl, Dictionary<string, string>? requestHeaders = null);

    /// <summary>
    /// Sends a synchronous POST request with content.
    /// </summary>
    /// <param name="endpointUrl">The relative or absolute URL of the resource.</param>
    /// <param name="httpContent">The <see cref="HttpContent"/> request content sent to the server.</param>
    /// <param name="requestHeaders">Optional dictionary of HTTP headers to include in the request.</param>
    /// <returns>An <see cref="ApiOperationResult{T}"/> containing the response body string.</returns>
    public ApiOperationResult<string> Post(string endpointUrl, HttpContent httpContent, Dictionary<string, string>? requestHeaders = null);

    /// <summary>
    /// Sends a synchronous PUT request.
    /// </summary>
    /// <param name="endpointUrl">The relative or absolute URL of the resource.</param>
    /// <param name="httpContent">The <see cref="HttpContent"/> request content sent to the server.</param>
    /// <param name="requestHeaders">Optional dictionary of HTTP headers to include in the request.</param>
    /// <returns>An <see cref="ApiOperationResult{T}"/> containing the response body string.</returns>
    public ApiOperationResult<string> Put(string endpointUrl, HttpContent httpContent, Dictionary<string, string>? requestHeaders = null);

    /// <summary>
    /// Sends a synchronous DELETE request.
    /// </summary>
    /// <param name="endpointUrl">The relative or absolute URL of the resource.</param>
    /// <param name="requestHeaders">Optional dictionary of HTTP headers to include in the request.</param>
    /// <returns>An <see cref="ApiOperationResult{T}"/> containing the response body string.</returns>
    public ApiOperationResult<string> Delete(string endpointUrl, Dictionary<string, string>? requestHeaders = null);
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
    /// Adds a default header that will be applied to all subsequent requests made by this client instance.
    /// </summary>
    /// <param name="key">The header name.</param>
    /// <param name="value">The header value.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating success.</returns>
    public NullOperationResult AddDefaultHeader(string key, string value);
    #endregion

    /// <summary>
    /// Debugging or logging method to output current <see cref="ApiClientSettings"/> to the console or logs.
    /// </summary>
    /// <param name="calledFromManager">Indicates if the call originated from a management orchestrator.</param>
    public void OutputRuntimeSettings(bool calledFromManager = false);
}

public enum RESTApiMediaTypes
{
    Json,
    Xml,
    PlainText,
    WWW_UrlEncoded
}