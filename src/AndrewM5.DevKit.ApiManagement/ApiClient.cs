using AndrewM5.DevKit.ApiManagement.Abstractions.Settings;
using AndrewM5.DevKit.ApiManagement.Contracts.Interfaces;
using AndrewM5.DevKit.Core;
using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.CredentialManagement.Contracts.Interfaces;
using AndrewM5.DevKit.Logging.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;

namespace AndrewM5.DevKit.ApiManagement;

public class ApiClient : IApiClient
{
    public string ClientName { get; set; }

    public ApiClientSettings RuntimeSettings { get; private set; }
    public IApiClientMetrics ClientMetrics => _metrics;

    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _rateLimiter;
    private readonly ApiClientMetrics _metrics;
    private readonly ICustomLogger? _logger;

    private const string NoSecretStore = "SecretStore has not been set. Call SetSecretStore()";
    private readonly string _secretStoreFileName;
    private ISecretStore? _secretStore;

    public ApiClient(IApiManager apiManager, string clientName, ApiClientSettings clientSettings, HttpClient httpClient, ICustomLogger? logger = null)
    {
        ClientName = clientName;

        _secretStoreFileName = $"ApiClient({ClientName})";
        _logger = logger;

        RuntimeSettings = clientSettings;

        if (RuntimeSettings.RequestCountBeforeRateLimiting < 0)
        {
            RuntimeSettings.RequestCountBeforeRateLimiting = int.MaxValue;
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
            RuntimeSettings.HttpTimeout_Seconds = apiManager.RuntimeSettings.Default_HttpTimeout_Seconds;
        }

        _httpClient = httpClient;
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

        _rateLimiter = new SemaphoreSlim(RuntimeSettings.RequestCountBeforeRateLimiting);
        _metrics = new ApiClientMetrics();
    }

    #region Asyncronous Methods
    public async Task<ApiOperationResult<string>> GetAsync(string endpointUrl, Dictionary<string, string>? requestHeaders = null)
    {
        var result = new ApiOperationResult<string>();

        try
        {
            string finalUrl = RuntimeSettings.BaseUrl.TrimEnd('/') + "/" + endpointUrl;

            result.SetRequestUrl(finalUrl);

            using (var request = new HttpRequestMessage(HttpMethod.Get, finalUrl))
            {
                var addRequestHeaders = AddRequestHeaders(request, requestHeaders);
                if (!addRequestHeaders.MethodSuccess)
                {
                    throw addRequestHeaders.Exception;
                }

                return await SendRequestAsync(request);
            }
        }
        catch (Exception ex)
        {
            return result.SetApiFailure(HttpStatusCode.InternalServerError, ex);
        }
    }

    public async Task<ApiOperationResult<string>> PutAsync(string endpointUrl, HttpContent httpContent, Dictionary<string, string>? requestHeaders = null)
    {
        var result = new ApiOperationResult<string>();

        try
        {
            string finalUrl = RuntimeSettings.BaseUrl.TrimEnd('/') + "/" + endpointUrl;

            result.SetRequestUrl(finalUrl);

            using (var request = new HttpRequestMessage(HttpMethod.Put, finalUrl))
            {
                var addRequestHeaders = AddRequestHeaders(request, requestHeaders);
                if (!addRequestHeaders.MethodSuccess)
                {
                    throw addRequestHeaders.Exception;
                }

                request.Content = httpContent;

                return await SendRequestAsync(request);
            }
        }
        catch (Exception ex)
        {
            return result.SetApiFailure(HttpStatusCode.InternalServerError, ex);
        }
    }

    public async Task<ApiOperationResult<string>> PostAsync(string endpointUrl, HttpContent httpContent, Dictionary<string, string>? requestHeaders = null)
    {
        var result = new ApiOperationResult<string>();

        try
        {
            string finalUrl = RuntimeSettings.BaseUrl.TrimEnd('/') + "/" + endpointUrl;

            result.SetRequestUrl(finalUrl);

            using (var request = new HttpRequestMessage(HttpMethod.Post, finalUrl))
            {
                var addRequestHeaders = AddRequestHeaders(request, requestHeaders);
                if (!addRequestHeaders.MethodSuccess)
                {
                    throw addRequestHeaders.Exception;
                }

                request.Content = httpContent;

                return await SendRequestAsync(request);
            }
        }
        catch (Exception ex)
        {
            return result.SetApiFailure(HttpStatusCode.InternalServerError, ex);
        }
    }

    public async Task<ApiOperationResult<string>> PostAsync(string endpointUrl, Dictionary<string, string>? requestHeaders = null)
    {
        var result = new ApiOperationResult<string>();

        try
        {
            string finalUrl = RuntimeSettings.BaseUrl.TrimEnd('/') + "/" + endpointUrl;

            result.SetRequestUrl(finalUrl);

            using (var request = new HttpRequestMessage(HttpMethod.Post, finalUrl))
            {
                var addRequestHeaders = AddRequestHeaders(request, requestHeaders);
                if (!addRequestHeaders.MethodSuccess)
                {
                    throw addRequestHeaders.Exception;
                }

                return await SendRequestAsync(request);
            }
        }
        catch (Exception ex)
        {
            return result.SetApiFailure(HttpStatusCode.InternalServerError, ex);
        }
    }

    public async Task<ApiOperationResult<string>> DeleteAsync(string endpointUrl, Dictionary<string, string>? requestHeaders = null)
    {
        var result = new ApiOperationResult<string>();

        try
        {
            string finalUrl = RuntimeSettings.BaseUrl.TrimEnd('/') + "/" + endpointUrl;

            result.SetRequestUrl(finalUrl);

            using (var request = new HttpRequestMessage(HttpMethod.Delete, finalUrl))
            {
                var addRequestHeaders = AddRequestHeaders(request, requestHeaders);
                if (!addRequestHeaders.MethodSuccess)
                {
                    throw addRequestHeaders.Exception;
                }

                return await SendRequestAsync(request);
            }
        }
        catch (Exception ex)
        {
            return result.SetApiFailure(HttpStatusCode.InternalServerError, ex);
        }
    }

    private async Task<ApiOperationResult<string>> SendRequestAsync(HttpRequestMessage request)
    {
        var result = new ApiOperationResult<string>();

        HttpMetricNames metricName = GetMetricName(request.Method);

        try
        {
            result.SetRequestUrl(request.RequestUri!.ToString());

            await _rateLimiter.WaitAsync();
            try
            {
                using (HttpResponseMessage response = await _httpClient.SendAsync(request))
                {
                    string responseBody = await response.Content.ReadAsStringAsync();
                    HttpStatusCode statusEnum = response.StatusCode;
                    int statusCode = (int)statusEnum;

                    try
                    {
                        response.EnsureSuccessStatusCode();

                        _metrics.IncrementMetric(metricName);

                        return result.SetApiSuccess(responseBody, statusEnum, $"HTTP {statusCode} {statusEnum}");
                    }
                    catch (HttpRequestException ex)
                    {
                        _metrics.IncrementMetric(metricName);
                        _metrics.IncrementFailure();

                        return result.SetApiFailure(statusEnum, ex, responseBody, $"Request failed with HTTP {statusCode} {statusEnum}");
                    }
                }
            }
            finally
            {
                _rateLimiter.Release();
            }
        }
        catch (Exception ex)
        {
            _metrics.IncrementMetric(metricName);
            _metrics.IncrementFailure();

            return result.SetApiFailure(HttpStatusCode.ServiceUnavailable, ex, "Unexpected request error (no HTTP response recieved)");
        }
        finally
        {
            await GCManager.CallGC_Collect("API Request");
        }
    }
    #endregion

    #region Syncronous Methods

    public ApiOperationResult<string> Get(string endpointUrl, Dictionary<string, string>? requestHeaders = null)
    {
        return GetAsync(endpointUrl, requestHeaders).GetAwaiter().GetResult();
    }

    public ApiOperationResult<string> Put(string endpointUrl, HttpContent httpContent, Dictionary<string, string>? requestHeaders = null)
    {
        return PutAsync(endpointUrl, httpContent, requestHeaders).GetAwaiter().GetResult();
    }

    public ApiOperationResult<string> Post(string endpointUrl, HttpContent httpContent, Dictionary<string, string>? requestHeaders = null)
    {
        return PostAsync(endpointUrl, httpContent, requestHeaders).GetAwaiter().GetResult();
    }

    public ApiOperationResult<string> Post(string endpointUrl, Dictionary<string, string>? requestHeaders = null)
    {
        return PostAsync(endpointUrl, requestHeaders).GetAwaiter().GetResult();
    }

    public ApiOperationResult<string> Delete(string endpointUrl, Dictionary<string, string>? requestHeaders = null)
    {
        return DeleteAsync(endpointUrl, requestHeaders).GetAwaiter().GetResult();
    }
    #endregion

    #region Helper Methods
    public OperationResult<HttpContent> CreateHttpContent(RESTApiMediaTypes mediaType, string data, Encoding? encoding = null)
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

    private OperationResult<string> GetMediaTypeString(RESTApiMediaTypes mediaType)
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

    private OperationResult<bool> AddRequestHeaders(HttpRequestMessage request, Dictionary<string, string>? requestHeaders = null)
    {
        var result = new OperationResult<bool>();

        try
        {
            if (requestHeaders != null && requestHeaders.Count > 0)
            {
                List<Exception> errors = new List<Exception>();

                foreach (var header in requestHeaders)
                {
                    try
                    {
                        var key = header.Key.Trim().ToLowerInvariant() ?? string.Empty;
                        var value = header.Value?.Trim() ?? string.Empty;

                        switch (key)
                        {
                            case "content-type":
                                if (request.Content == null)
                                {
                                    throw new InvalidOperationException("Cannot set Content-Type when HttpContent is null.");
                                }

                                request.Content.Headers.ContentType = new MediaTypeHeaderValue(value);
                                break;
                            case "authorization":
                                // Check for common schemes: Basic, Bearer, Digest, or custom
                                switch (value)
                                {
                                    case "basic":
                                        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", value.Substring(6));
                                        break;
                                    case "bearer":
                                        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", value.Substring(7));
                                        break;
                                    default:
                                        var split = value.IndexOf(' ');
                                        if (split > 0)
                                        {
                                            var scheme = value.Substring(0, split);
                                            var parameter = value.Substring(split + 1);
                                            request.Headers.Authorization = new AuthenticationHeaderValue(scheme, parameter);
                                        }
                                        else
                                        {
                                            // If only a token without scheme, treat as Bearer
                                            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", value);
                                        }
                                        break;
                                }
                                break;
                            case "user-agent":
                                request.Headers.UserAgent.ParseAdd(header.Value);
                                break;
                            default:
                                if (request.Headers.Contains(header.Key))
                                {
                                    request.Headers.Remove(header.Key); // overwrite existing header
                                }

                                request.Headers.Add(header.Key, header.Value);
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex);
                    }
                }

                if (errors.Count > 0)
                {
                    throw new AggregateException(errors);
                }
            }

            return result.SetMethodSuccess(true);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }
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
    #endregion

    #region Credentials
    public void SetSecretStore(ISecretStore secretStore)
    {
        _secretStore = secretStore;
    }
    public NullOperationResult SetCredentials(string username, string password)
    {
        var result = new NullOperationResult();

        try
        {
            if (_secretStore == null)
            {
                throw new ArgumentNullException(NoSecretStore);
            }

            var setUsernameKey = _secretStore.SetKey(_secretStoreFileName, "username", username);
            if (!setUsernameKey.MethodSuccess)
            {
                throw setUsernameKey.Exception;
            }

            var setPasswordKey = _secretStore.SetKey(_secretStoreFileName, "password", password);
            if (!setPasswordKey.MethodSuccess)
            {
                throw setPasswordKey.Exception;
            }

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    public OperationResult<string> GetUsername()
    {
        return GetCredentials("username", RuntimeSettings.Username);
    }

    public OperationResult<string> GetPassword()
    {
        return GetCredentials("password", RuntimeSettings.Password);
    }

    public NullOperationResult DeleteCredential(string key)
    {
        var result = new NullOperationResult();

        try
        {
            if (_secretStore == null)
            {
                throw new ArgumentNullException(NoSecretStore);
            }

            return _secretStore.DeleteKey(_secretStoreFileName, key);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    public NullOperationResult DeleteAllCredentials()
    {
        var result = new NullOperationResult();

        try
        {
            if (_secretStore == null)
            {
                throw new ArgumentNullException(NoSecretStore);
            }

            return _secretStore.DeleteSecret(_secretStoreFileName);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    private OperationResult<string> GetCredentials(string key, string defaultStr)
    {
        var result = new OperationResult<string>();

        try
        {
            if (_secretStore != null)
            {
                var getKey = _secretStore.GetKey(_secretStoreFileName, key);
                if (!getKey.MethodSuccess)
                {
                    throw getKey.Exception;
                }

                return result.SetMethodSuccess(getKey.Result);
            }
            else
            {
                return result.SetMethodSuccess(defaultStr);
            }
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }
    #endregion

    public void OutputRuntimeSettings(bool calledFromManager = false)
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
                _logger?.LogDebug($"{indent}{property.Name}:");

                foreach (var kvp in dict)
                {
                    _logger?.LogDebug($"{indent}  {kvp.Key}: {kvp.Value}");
                }
            }
            else
            {
                _logger?.LogDebug($"{indent}{property.Name}: {value}");
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        _rateLimiter?.Dispose();
        _httpClient?.Dispose();

        return ValueTask.CompletedTask;
    }
}