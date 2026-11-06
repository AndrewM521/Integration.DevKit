/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.Core;
using Integration.DevKit.RESTApiMgmt.Interfaces;
using Integration.DevKit.RESTApiMgmt.Settings;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Reflection;
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

    private const double MaxRetryDelay_Milliseconds = 30000;

    private readonly HttpClient _httpClient;
    private readonly ApiManager _apiManager;
    private SemaphoreSlim _rateLimiter = null!;
    private readonly ApiClientMetrics _metrics;
    private readonly ILogger? _logger;

    private IAuthStrategy? _authStrategy;
    private readonly AuthClient _authClient;

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

        Initialize();
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
            if (RuntimeSettings.MaxConcurrentRequests < 0)
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

            RuntimeSettings.MaxRetries = Math.Max(0, RuntimeSettings.MaxRetries ?? _apiManager.RuntimeSettings.Default_MaxRetries);
            RuntimeSettings.RetryBaseDelay_Milliseconds = Math.Max(0, RuntimeSettings.RetryBaseDelay_Milliseconds ?? _apiManager.RuntimeSettings.Default_RetryBaseDelay_Milliseconds);

            _httpClient.BaseAddress = new Uri(RuntimeSettings.BaseUrl);

            if (RuntimeSettings.HttpTimeout_Seconds != 0)
            {
                _httpClient.Timeout = TimeSpan.FromSeconds((double)RuntimeSettings.HttpTimeout_Seconds);
            }
            else
            {
                _httpClient.Timeout = Timeout.InfiniteTimeSpan;
            }

            foreach (var header in RuntimeSettings.DefaultHeaders)
            {
                AddDefaultHeader(header.Key, header.Value);
            }

            var oldRateLimiter = _rateLimiter;
            _rateLimiter = new SemaphoreSlim(RuntimeSettings.MaxConcurrentRequests);
            oldRateLimiter?.Dispose();

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
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
        return await SendRequestOrchestratorAsync(HttpMethod.Get, httpContent, content =>
            ApiRequest.GetAsync(_httpClient, endpointUrl, content, requestHeaders, _authStrategy, _authClient));
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
        return await SendRequestOrchestratorAsync(HttpMethod.Put, httpContent, content =>
            ApiRequest.PutAsync(_httpClient, endpointUrl, content!, requestHeaders, _authStrategy, _authClient));
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
        return await SendRequestOrchestratorAsync(HttpMethod.Post, httpContent, content =>
            ApiRequest.PostAsync(_httpClient, endpointUrl, content, requestHeaders, _authStrategy, _authClient));
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
        return await SendRequestOrchestratorAsync(HttpMethod.Delete, httpContent, content =>
            ApiRequest.DeleteAsync(_httpClient, endpointUrl, content, requestHeaders, _authStrategy, _authClient));
    }

    // A generic wrapper to handle the "Orchestration" (Metrics + Rate Limiting + Retries)
    private async Task<ApiOperationResult<string>> SendRequestOrchestratorAsync(HttpMethod method, HttpContent? httpContent, Func<HttpContent?, Task<ApiOperationResult<string>>> action)
    {
        HttpMetricNames metricName = GetMetricName(method);
        int maxRetries = RuntimeSettings.MaxRetries ?? 0;
        
        await _rateLimiter.WaitAsync();

        try
        {
            // Each request disposes the content it sends, so when retrying, buffer the body once and send a fresh copy per attempt.
            byte[]? body = null;

            if (maxRetries > 0 && httpContent != null)
            {
                try
                {
                    body = await httpContent.ReadAsByteArrayAsync();
                }
                catch
                {
                    // Unreadable content (e.g. already disposed): send it once and let ApiRequest report the failure.
                    maxRetries = 0;
                }
            }

            ApiOperationResult<string> result;
            int attempt = 0;

            while (true)
            {
                HttpContent? attemptContent = body == null ? httpContent : CopyContent(httpContent!, body);

                result = await action(attemptContent);

                if (result.MethodSuccess || attempt >= maxRetries || !IsTransientFailure(result.StatusCode))
                {
                    break;
                }

                attempt++;

                TimeSpan delay = GetRetryDelay(attempt);

                _logger?.LogWarning($"Api Client '{ClientName}': {method} {result.RequestUrl} failed with {(int)result.StatusCode} {result.StatusCode}. Retry {attempt} of {maxRetries} in {delay.TotalMilliseconds:F0} ms");

                await Task.Delay(delay);
            }

            if (body != null)
            {
                httpContent!.Dispose();
            }

            _metrics.IncrementMetric(metricName);

            if (!result.MethodSuccess)
            {
                _metrics.IncrementFailure();
            }

            return result;
        }
        finally
        {
            _rateLimiter.Release();

            await GCManager.CallGC_Collect("API Request");
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
                throw getMediaTypeStr.Exception;
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
    /// Returns whether a failed request is worth retrying: network errors and timeouts (reported as 503 and 500
    /// by <see cref="ApiRequest.SendRequestAsync"/>), plus HTTP 408, 429, 500, 502, 503 and 504.
    /// </summary>
    private static bool IsTransientFailure(HttpStatusCode statusCode)
    {
        switch (statusCode)
        {
            case HttpStatusCode.RequestTimeout:
            case HttpStatusCode.TooManyRequests:
            case HttpStatusCode.InternalServerError:
            case HttpStatusCode.BadGateway:
            case HttpStatusCode.ServiceUnavailable:
            case HttpStatusCode.GatewayTimeout:
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// Exponential backoff: the base delay doubled for each earlier retry, with ±20% jitter, capped at 30 seconds.
    /// </summary>
    private TimeSpan GetRetryDelay(int attempt)
    {
        double delay_Milliseconds = (RuntimeSettings.RetryBaseDelay_Milliseconds ?? 0) * Math.Pow(2, attempt - 1);
        double jitter = 0.8 + (Random.Shared.NextDouble() * 0.4);

        return TimeSpan.FromMilliseconds(Math.Min(delay_Milliseconds * jitter, MaxRetryDelay_Milliseconds));
    }

    /// <summary>
    /// Creates a fresh copy of buffered request content, keeping its content headers (e.g. Content-Type).
    /// </summary>
    private static HttpContent CopyContent(HttpContent original, byte[] body)
    {
        var copy = new ByteArrayContent(body);

        foreach (var header in original.Headers)
        {
            // ByteArrayContent computes its own length.
            if (!header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase))
            {
                copy.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }
        }

        return copy;
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
    public NullOperationResult AddDefaultHeader(string key, string value)
    {
        var result = new NullOperationResult();

        try
        {
            if (_httpClient.DefaultRequestHeaders.Contains(key))
            {
                _httpClient.DefaultRequestHeaders.Remove(key);
            }

            _httpClient.DefaultRequestHeaders.Add(key, value);

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
    /// Logging method to output current <see cref="ApiClientSettings"/> to the logs.
    /// </summary>
    /// <param name="calledFromManager">Indicates if the call originated from a management orchestrator.</param>
    public void LogRuntimeSettings(bool calledFromManager = false)
    {
        string indent;

        if (!calledFromManager)
        {
            _logger?.LogDebug($"--- Api Client Settings ---");
            indent = "";
        }
        else
        {
            indent = "    ";
        }

        _logger?.LogDebug($"{indent}Client: {ClientName}");

        indent += "  ";

        Type type = RuntimeSettings.GetType();
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            object? value = property.GetValue(RuntimeSettings);

            if (value is Dictionary<string, string> dict)
            {
                _logger?.LogTrace($"{indent}{property.Name}:");

                foreach (var kvp in dict)
                {
                    _logger?.LogTrace($"{indent}  {kvp.Key}: {kvp.Value}");
                }
            }
            else
            {
                _logger?.LogTrace($"{indent}{property.Name}: {value}");
            }
        }
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