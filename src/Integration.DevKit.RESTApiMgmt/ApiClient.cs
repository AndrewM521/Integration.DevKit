/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.Core;
using Integration.DevKit.RESTApiMgmt.Interfaces;
using Integration.DevKit.RESTApiMgmt.Settings;
using Microsoft.Extensions.Logging;
using System.Runtime.ExceptionServices;
using System.Text;

namespace Integration.DevKit.RESTApiMgmt;

/// <summary>
/// Specialized HTTP client capable of performing RESTful operations,
/// managing credentials, and tracking client-side metrics.
/// </summary>
public class ApiClient : IAsyncDisposable
{
    /// <summary>
    /// Gets or sets the display name for this API client instance.
    /// </summary>
    public string ClientName { get; set; }

    /// <summary>
    /// Gets the runtime configuration used by this client.
    /// </summary>
    public ApiClientSettings RuntimeSettings { get; private set; }

    /// <summary>
    /// Gets the metrics collector associated with this client.
    /// </summary>
    public ApiClientMetrics ClientMetrics => _metrics;

    private HttpClient _httpClient;
    private readonly ApiManager _apiManager;
    private SemaphoreSlim _rateLimiter = null!;
    private readonly ApiClientMetrics _metrics;
    private readonly ILogger? _logger;

    private IAuthStrategy? _authStrategy;
    private AuthClient _authClient;

    private readonly object _initializeLock = new object();
    // The settings headers last applied (name -> value), so Initialize can tell whether they changed
    private readonly Dictionary<string, string> _appliedSettingsHeaders = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    private bool _isSetUp;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiClient"/> class.
    /// </summary>
    /// <param name="apiManager">The parent manager used to resolve default settings.</param>
    /// <param name="clientName">The unique name for this client instance.</param>
    /// <param name="clientSettings">The specific configuration for this client.</param>
    /// <param name="httpClient">The underlying <see cref="HttpClient"/> instance to use for requests.</param>
    /// <param name="logger">Optional logger for debugging and runtime info.</param>
    internal ApiClient(ApiManager apiManager, string clientName, ApiClientSettings clientSettings, HttpClient httpClient, ILogger? logger = null)
    {
        ClientName = clientName;

        _logger = logger;
        _apiManager = apiManager;

        RuntimeSettings = clientSettings;
        _httpClient = httpClient;
        _metrics = new ApiClientMetrics();
        _authClient = new AuthClient(_httpClient, CreateHeaderFreeHttpClient);

        // A failed setup leaves no rate limiter, so the first request would crash with a NullReferenceException
        var initialize = Initialize();
        if (!initialize.MethodSuccess)
        {
            ExceptionDispatchInfo.Throw(initialize.Exception);
        }
    }

    /// <summary>
    /// Re-derives everything this client caches from <see cref="RuntimeSettings"/> (the underlying
    /// <see cref="HttpClient"/>'s base address, timeout, and default headers, plus the concurrent-request
    /// rate limiter). Call this after mutating <see cref="RuntimeSettings"/> in place so the change takes effect.
    /// </summary>
    /// <returns>A <see cref="NullOperationResult"/> indicating success or failure.</returns>
    /// <remarks>
    /// Re-derives everything this client caches from <see cref="RuntimeSettings"/> at construction time
    /// (the <see cref="HttpClient"/>'s base address, timeout, and default headers, plus the concurrent-request
    /// rate limiter). Call this after mutating <see cref="RuntimeSettings"/> in place so the change takes effect.
    /// </remarks>
    public NullOperationResult Initialize()
    {
        var result = new NullOperationResult();

        try
        {
            // Serialized so two concurrent calls can't both swap the HttpClient or interleave header edits
            lock (_initializeLock)
            {
                InitializeCore();
            }

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    private void InitializeCore()
    {
        // 0 would make a semaphore with no slots, so every request would wait forever
        if (RuntimeSettings.MaxConcurrentRequests <= 0)
        {
            RuntimeSettings.MaxConcurrentRequests = int.MaxValue;
        }

        if (RuntimeSettings.HttpTimeout_Seconds != null)
        {
            if (RuntimeSettings.HttpTimeout_Seconds < 0)
            {
                RuntimeSettings.HttpTimeout_Seconds = 0;
            }
        }
        else
        {
            RuntimeSettings.HttpTimeout_Seconds = _apiManager.RuntimeSettings.Default_HttpTimeout_Seconds;
        }

        Uri baseAddress = new Uri(RuntimeSettings.BaseUrl);

        // 0 means no timeout
        TimeSpan timeout = Timeout.InfiniteTimeSpan;

        if (RuntimeSettings.HttpTimeout_Seconds != 0)
        {
            timeout = TimeSpan.FromSeconds((double)RuntimeSettings.HttpTimeout_Seconds!);
        }

        if (!_isSetUp)
        {
            // Called from the constructor: no request has used this client yet, so it can be set up in place
            _httpClient.BaseAddress = baseAddress;
            _httpClient.Timeout = timeout;
            ApplySettingsHeaders(_httpClient);
            _isSetUp = true;
        }
        else if (_httpClient.BaseAddress != baseAddress || _httpClient.Timeout != timeout || !SettingsHeadersMatch())
        {
            SwapInNewHttpClient(baseAddress, timeout);
        }

        // Not disposed: in-flight requests still release the limiter they captured.
        // SemaphoreSlim only needs disposing when AvailableWaitHandle is used, which it isn't here.
        _rateLimiter = new SemaphoreSlim(RuntimeSettings.MaxConcurrentRequests);
    }

    /// <summary>
    /// Builds a complete new <see cref="HttpClient"/> from the factory and only then makes it the current one.
    /// Requests may be in flight on the current client: <see cref="HttpClient"/> refuses base address and timeout
    /// changes after its first request, and its default headers are not safe to edit while requests are being sent.
    /// </summary>
    private void SwapInNewHttpClient(Uri baseAddress, TimeSpan timeout)
    {
        var newClient = _apiManager.CreateHeaderFreeHttpClient();
        newClient.BaseAddress = baseAddress;
        newClient.Timeout = timeout;

        // Keep headers added through AddDefaultHeader. Settings headers are applied fresh from the current
        // settings below, so ones deleted from the settings are dropped.
        foreach (var header in _httpClient.DefaultRequestHeaders)
        {
            if (!_appliedSettingsHeaders.ContainsKey(header.Key))
            {
                newClient.DefaultRequestHeaders.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        ApplySettingsHeaders(newClient);

        // The old client is not disposed: requests already in flight may still be using it. Factory-made clients
        // don't own their pooled handler, so dropping the old one releases nothing that needs disposing.
        _authClient = new AuthClient(newClient, CreateHeaderFreeHttpClient);
        _httpClient = newClient;

        _logger?.LogDebug($"Api client '{ClientName}' switched to a new HttpClient for base address '{baseAddress}' and timeout {timeout}.");
    }

    /// <summary>
    /// Adds every header from <see cref="ApiClientSettings.DefaultHeaders"/> to <paramref name="client"/>
    /// and remembers them, so a later <see cref="Initialize"/> can tell whether they changed.
    /// </summary>
    private void ApplySettingsHeaders(HttpClient client)
    {
        _appliedSettingsHeaders.Clear();

        foreach (var header in RuntimeSettings.DefaultHeaders)
        {
            // Remembered even if it can't be applied, or every Initialize would see a change and swap clients
            _appliedSettingsHeaders[header.Key] = header.Value;

            var addHeader = SetDefaultHeader(client, header.Key, header.Value);
            if (!addHeader.MethodSuccess)
            {
                // e.g. Content-Type belongs on the request content, so HttpClient refuses it as a default header
                _logger?.LogWarning(addHeader.Exception, $"Api client '{ClientName}' could not apply default header '{header.Key}'; requests are sent without it.");
            }
        }
    }

    /// <summary>
    /// Whether <see cref="ApiClientSettings.DefaultHeaders"/> still holds exactly the headers last applied.
    /// </summary>
    private bool SettingsHeadersMatch()
    {
        if (RuntimeSettings.DefaultHeaders.Count != _appliedSettingsHeaders.Count)
        {
            return false;
        }

        foreach (var header in RuntimeSettings.DefaultHeaders)
        {
            if (!_appliedSettingsHeaders.TryGetValue(header.Key, out var appliedValue) || appliedValue != header.Value)
            {
                return false;
            }
        }

        return true;
    }

    #region Asyncronous Methods
    /// <summary>
    /// Sends an asynchronous GET request to the specified endpoint.
    /// </summary>
    /// <param name="endpointUrl">The relative or absolute URL of the resource.</param>
    /// <param name="httpContent">Optional <see cref="HttpContent"/> request content sent to the server.</param>
    /// <param name="requestHeaders">Optional dictionary of HTTP headers to include in the request.</param>
    /// <returns>A task representing the result as an <see cref="ApiOperationResult{T}"/> containing the response body string.</returns>
    public async Task<ApiOperationResult<string>> GetAsync(string endpointUrl, HttpContent? httpContent = null, Dictionary<string, string>? requestHeaders = null)
    {
        return await SendRequestOrchestratorAsync(HttpMethod.Get, () =>
            ApiRequest.GetAsync(_httpClient, endpointUrl, httpContent, requestHeaders, _authStrategy, _authClient)).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends an asynchronous PUT request to the specified endpoint.
    /// </summary>
    /// <param name="endpointUrl">The relative or absolute URL of the resource.</param>
    /// <param name="httpContent">The <see cref="HttpContent"/> request content sent to the server.</param>
    /// <param name="requestHeaders">Optional dictionary of HTTP headers to include in the request.</param>
    /// <returns>A task representing the result as an <see cref="ApiOperationResult{T}"/> containing the response body string.</returns>
    public async Task<ApiOperationResult<string>> PutAsync(string endpointUrl, HttpContent httpContent, Dictionary<string, string>? requestHeaders = null)
    {
        return await SendRequestOrchestratorAsync(HttpMethod.Put, () =>
            ApiRequest.PutAsync(_httpClient, endpointUrl, httpContent, requestHeaders, _authStrategy, _authClient)).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends an asynchronous POST request with an optional specified body to the specified endpoint.
    /// </summary>
    /// <param name="endpointUrl">The relative or absolute URL of the resource.</param>
    /// <param name="httpContent">Optional <see cref="HttpContent"/> request content sent to the server.</param>
    /// <param name="requestHeaders">Optional dictionary of HTTP headers to include in the request.</param>
    /// <returns>A task representing the result as an <see cref="ApiOperationResult{T}"/> containing the response body string.</returns>
    public async Task<ApiOperationResult<string>> PostAsync(string endpointUrl, HttpContent? httpContent = null, Dictionary<string, string>? requestHeaders = null)
    {
        return await SendRequestOrchestratorAsync(HttpMethod.Post, () =>
            ApiRequest.PostAsync(_httpClient, endpointUrl, httpContent, requestHeaders, _authStrategy, _authClient)).ConfigureAwait(false);
    }

    /// <summary>
    /// Sends an asynchronous DELETE request to the specified endpoint.
    /// </summary>
    /// <param name="endpointUrl">The relative or absolute URL of the resource.</param>
    /// <param name="httpContent">Optional <see cref="HttpContent"/> request content sent to the server.</param>
    /// <param name="requestHeaders">Optional dictionary of HTTP headers to include in the request.</param>
    /// <returns>A task representing the result as an <see cref="ApiOperationResult{T}"/> containing the response body string.</returns>
    public async Task<ApiOperationResult<string>> DeleteAsync(string endpointUrl, HttpContent? httpContent = null, Dictionary<string, string>? requestHeaders = null)
    {
        return await SendRequestOrchestratorAsync(HttpMethod.Delete, () =>
            ApiRequest.DeleteAsync(_httpClient, endpointUrl, httpContent, requestHeaders, _authStrategy, _authClient)).ConfigureAwait(false);
    }

    // A generic wrapper to handle the "Orchestration" (Metrics + Rate Limiting)
    private async Task<ApiOperationResult<string>> SendRequestOrchestratorAsync(HttpMethod method, Func<Task<ApiOperationResult<string>>> action)
    {
        HttpMetricNames metricName = GetMetricName(method);
        
        // Capture it so a concurrent Initialize() swapping the limiter can't make this release the wrong one
        var rateLimiter = _rateLimiter;
        await rateLimiter.WaitAsync().ConfigureAwait(false);

        try
        {
            var result = await action().ConfigureAwait(false);

            _metrics.IncrementMetric(metricName);

            if (!result.MethodSuccess)
            {
                _metrics.IncrementFailure();
            }

            return result;
        }
        finally
        {
            rateLimiter.Release();

            await GCManager.CallGC_Collect("API Request").ConfigureAwait(false);
        }
    }
    #endregion

    #region Syncronous Methods

    /// <summary>
    /// Sends a synchronous GET request to the specified endpoint.
    /// </summary>
    /// <param name="endpointUrl">The relative or absolute URL of the resource.</param>
    /// <param name="httpContent">Optional <see cref="HttpContent"/> request content sent to the server.</param>
    /// <param name="requestHeaders">Optional dictionary of HTTP headers to include in the request.</param>
    /// <returns>An <see cref="ApiOperationResult{T}"/> containing the response body string.</returns>
    public ApiOperationResult<string> Get(string endpointUrl, HttpContent? httpContent = null, Dictionary<string, string>? requestHeaders = null)
    {
        return GetAsync(endpointUrl, httpContent, requestHeaders).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Sends a synchronous PUT request to the specified endpoint.
    /// </summary>
    /// <param name="endpointUrl">The relative or absolute URL of the resource.</param>
    /// <param name="httpContent">The <see cref="HttpContent"/> request content sent to the server.</param>
    /// <param name="requestHeaders">Optional dictionary of HTTP headers to include in the request.</param>
    /// <returns>An <see cref="ApiOperationResult{T}"/> containing the response body string.</returns>
    public ApiOperationResult<string> Put(string endpointUrl, HttpContent httpContent, Dictionary<string, string>? requestHeaders = null)
    {
        return PutAsync(endpointUrl, httpContent, requestHeaders).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Sends a synchronous POST request with an optional specified body to the specified endpoint.
    /// </summary>
    /// <param name="endpointUrl">The relative or absolute URL of the resource.</param>
    /// <param name="httpContent">Optional <see cref="HttpContent"/> request content sent to the server.</param>
    /// <param name="requestHeaders">Optional dictionary of HTTP headers to include in the request.</param>
    /// <returns>An <see cref="ApiOperationResult{T}"/> containing the response body string.</returns>
    public ApiOperationResult<string> Post(string endpointUrl, HttpContent? httpContent = null, Dictionary<string, string>? requestHeaders = null)
    {
        return PostAsync(endpointUrl, httpContent, requestHeaders).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Sends a synchronous DELETE request to the specified endpoint.
    /// </summary>
    /// <param name="endpointUrl">The relative or absolute URL of the resource.</param>
    /// <param name="httpContent">Optional <see cref="HttpContent"/> request content sent to the server.</param>
    /// <param name="requestHeaders">Optional dictionary of HTTP headers to include in the request.</param>
    /// <returns>An <see cref="ApiOperationResult{T}"/> containing the response body string.</returns>
    public ApiOperationResult<string> Delete(string endpointUrl, HttpContent? httpContent = null, Dictionary<string, string>? requestHeaders = null)
    {
        return DeleteAsync(endpointUrl, httpContent, requestHeaders).GetAwaiter().GetResult();
    }
    #endregion

    /// <summary>
    /// Injects an <see cref="IAuthStrategy"/> that is applied to every outgoing request
    /// (e.g. an OAuth2 bearer token), independently of any Basic-style credentials.
    /// The strategy receives an <see cref="IAuthClient"/> for calling its authentication endpoint
    /// without re-entering itself.
    /// </summary>
    /// <param name="authStrategy">The strategy to apply, or <see langword="null"/> to stop applying one.</param>
    public void SetAuthStrategy(IAuthStrategy? authStrategy)
    {
        _authStrategy = authStrategy;
    }

    /// <summary>
    /// Formats data into an <see cref="HttpContent"/> object suitable for request bodies.
    /// </summary>
    /// <param name="mediaType">The intended <see cref="RESTApiMediaTypes"/>.</param>
    /// <param name="data">The raw string data to be converted.</param>
    /// <param name="encoding">The text <see cref="Encoding"/> to use. Defaults to UTF-8 if null.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the formatted <see cref="HttpContent"/>.</returns>
    public OperationResult<HttpContent> CreateHttpContent(RESTApiMediaTypes mediaType, string data, Encoding? encoding = null)
    {
        return BuildHttpContent(mediaType, data, encoding);
    }

    /// <summary>
    /// Shared implementation of <see cref="CreateHttpContent"/>, also used by <see cref="AuthClient"/>.
    /// </summary>
    internal static OperationResult<HttpContent> BuildHttpContent(RESTApiMediaTypes mediaType, string data, Encoding? encoding = null)
    {
        var result = new OperationResult<HttpContent>();

        if (encoding == null)
        {
            encoding = Encoding.UTF8;
        }

        try
        {
            var getMediaTypeStr = GetMediaTypeString(mediaType);
            if (!getMediaTypeStr.MethodSuccess)
            {
                ExceptionDispatchInfo.Throw(getMediaTypeStr.Exception);
            }

            StringContent content = new StringContent(data, encoding, getMediaTypeStr.Result);

            return result.SetMethodSuccess(content);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Creates the header-free client the <see cref="AuthClient"/> uses for absolute URLs on a different host.
    /// </summary>
    private HttpClient CreateHeaderFreeHttpClient()
    {
        var headerFreeClient = _apiManager.CreateHeaderFreeHttpClient();
        headerFreeClient.Timeout = _httpClient.Timeout;

        return headerFreeClient;
    }

    /// <summary>
    /// Adds a default header to the client that will be applied to all subsequent requests.
    /// </summary>
    /// <param name="key">The header name.</param>
    /// <param name="value">The header value.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating success.</returns>
    /// <remarks>
    /// This edits the live <see cref="HttpClient"/>, which is not safe while requests are being sent. Call it before
    /// the client is in use, or put the header in <see cref="ApiClientSettings.DefaultHeaders"/> and call
    /// <see cref="Initialize"/>, which swaps in a new client safely.
    /// </remarks>
    public NullOperationResult AddDefaultHeader(string key, string value)
    {
        return SetDefaultHeader(_httpClient, key, value);
    }

    private static NullOperationResult SetDefaultHeader(HttpClient client, string key, string value)
    {
        var result = new NullOperationResult();

        try
        {
            if (client.DefaultRequestHeaders.Contains(key))
            {
                client.DefaultRequestHeaders.Remove(key);
            }

            client.DefaultRequestHeaders.Add(key, value);

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Maps the <see cref="RESTApiMediaTypes"/> enum to its corresponding MIME type string.
    /// </summary>
    private static OperationResult<string> GetMediaTypeString(RESTApiMediaTypes mediaType)
    {
        var result = new OperationResult<string>();

        try
        {
            string mediaTypeStr;

            switch (mediaType)
            {
                case RESTApiMediaTypes.PlainText:
                    mediaTypeStr = "text/plain";
                    break;
                case RESTApiMediaTypes.Json:
                    mediaTypeStr = "application/json";
                    break;
                case RESTApiMediaTypes.Xml:
                    mediaTypeStr = "application/xml";
                    break;
                case RESTApiMediaTypes.WWW_UrlEncoded:
                    mediaTypeStr = "application/x-www-form-urlencoded";
                    break;
                default:
                    throw new Exception($"Invalid media type '{mediaType}'");
            }

            return result.SetMethodSuccess(mediaTypeStr);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Maps standard <see cref="HttpMethod"/> types to our custom <see cref="HttpMetricNames"/> enum.
    /// </summary>
    private HttpMetricNames GetMetricName(HttpMethod method)
    {
        if (method == HttpMethod.Get)
        {
            return HttpMetricNames.Get;
        }
        else if (method == HttpMethod.Post)
        {
            return HttpMetricNames.Post;
        }
        else if (method == HttpMethod.Put)
        {
            return HttpMetricNames.Put;
        }
        else if (method == HttpMethod.Delete)
        {
            return HttpMetricNames.Delete;
        }
        else if (method == HttpMethod.Patch)
        {
            return HttpMetricNames.Patch;
        }
        else if (method == HttpMethod.Head)
        {
            return HttpMetricNames.Head;
        }
        else if (method == HttpMethod.Options)
        {
            return HttpMetricNames.Options;
        }

        return HttpMetricNames.Other;
    }

    /// <summary>
    /// Disposes the client and rate limiter
    /// </summary>
    public ValueTask DisposeAsync()
    {
        _rateLimiter?.Dispose();
        _httpClient?.Dispose();

        return ValueTask.CompletedTask;
    }
}