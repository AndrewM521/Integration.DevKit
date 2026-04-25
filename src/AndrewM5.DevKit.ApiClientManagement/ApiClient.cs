using AndrewM5.DevKit.ApiClientManagement.Contracts.Interfaces;
using AndrewM5.DevKit.ApiClientManagement.Contracts.Options;
using AndrewM5.DevKit.Core;
using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.CredentialManagement.Contracts.Interfaces;
using AndrewM5.DevKit.CustomLogger.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Text;

namespace AndrewM5.DevKit.ApiClientManagement;

/// <summary>
/// Concrete Implementation of <see cref="IApiClient"/>
/// </summary>
public class ApiClient : IApiClient
{
    /// <inheritdoc/>
    public string ClientName { get; set; }

    /// <inheritdoc/>
    public ApiClientSettings RuntimeSettings { get; private set; }

    /// <inheritdoc/>
    public IApiClientMetrics ClientMetrics => _metrics;

    ApiClientSettings IApiClient.RuntimeSettings => throw new NotImplementedException();

    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _rateLimiter;
    private readonly ApiClientMetrics _metrics;
    private readonly ICustomLogger? _logger;

    private const string NoSecretStore = "SecretStore has not been set. Call SetSecretStore()";
    private readonly string _secretStoreFileName;
    private ISecretStore? _secretStore;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiClient"/> class.
    /// </summary>
    /// <param name="apiManager">The parent manager used to resolve default settings.</param>
    /// <param name="clientName">The unique name for this client instance.</param>
    /// <param name="clientSettings">The specific configuration for this client.</param>
    /// <param name="httpClient">The underlying <see cref="HttpClient"/> instance to use for requests.</param>
    /// <param name="logger">Optional logger for debugging and runtime info.</param>
    internal ApiClient(IApiManager apiManager, string clientName, ApiClientSettings clientSettings, HttpClient httpClient, ICustomLogger? logger = null)
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
    /// <inheritdoc/>
    public async Task<ApiOperationResult<string>> GetAsync(string endpointUrl, Dictionary<string, string>? requestHeaders = null)
    {
        return await SendRequestOrchestratorAsync(HttpMethod.Get, () =>
            ApiRequest.GetAsync(_httpClient, endpointUrl, requestHeaders));
    }

    /// <inheritdoc/>
    public async Task<ApiOperationResult<string>> PutAsync(string endpointUrl, HttpContent httpContent, Dictionary<string, string>? requestHeaders = null)
    {
        return await SendRequestOrchestratorAsync(HttpMethod.Put, () =>
            ApiRequest.PutAsync(_httpClient, endpointUrl, httpContent, requestHeaders));
    }

    /// <inheritdoc/>
    public async Task<ApiOperationResult<string>> PostAsync(string endpointUrl, HttpContent? httpContent = null, Dictionary<string, string>? requestHeaders = null)
    {
        return await SendRequestOrchestratorAsync(HttpMethod.Post, () =>
            ApiRequest.PostAsync(_httpClient, endpointUrl, httpContent, requestHeaders));
    }

    /// <inheritdoc/>
    public async Task<ApiOperationResult<string>> DeleteAsync(string endpointUrl, Dictionary<string, string>? requestHeaders = null)
    {
        return await SendRequestOrchestratorAsync(HttpMethod.Delete, () =>
            ApiRequest.DeleteAsync(_httpClient, endpointUrl, requestHeaders));
    }

    // A generic wrapper to handle the "Orchestration" (Metrics + Rate Limiting)
    private async Task<ApiOperationResult<string>> SendRequestOrchestratorAsync(HttpMethod method, Func<Task<ApiOperationResult<string>>> action)
    {
        HttpMetricNames metricName = GetMetricName(method);
        
        await _rateLimiter.WaitAsync();

        try
        {
            var result = await action();

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

    /// <inheritdoc/>
    public ApiOperationResult<string> Get(string endpointUrl, Dictionary<string, string>? requestHeaders = null)
    {
        return GetAsync(endpointUrl, requestHeaders).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public ApiOperationResult<string> Put(string endpointUrl, HttpContent httpContent, Dictionary<string, string>? requestHeaders = null)
    {
        return PutAsync(endpointUrl, httpContent, requestHeaders).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public ApiOperationResult<string> Post(string endpointUrl, HttpContent? httpContent = null, Dictionary<string, string>? requestHeaders = null)
    {
        return PostAsync(endpointUrl, httpContent, requestHeaders).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public ApiOperationResult<string> Delete(string endpointUrl, Dictionary<string, string>? requestHeaders = null)
    {
        return DeleteAsync(endpointUrl, requestHeaders).GetAwaiter().GetResult();
    }
    #endregion

    #region Helper Methods
    /// <inheritdoc/>
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

    /// <inheritdoc/>
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
    #endregion

    #region Credentials
    /// <inheritdoc/>
    public void SetSecretStore(ISecretStore secretStore)
    {
        _secretStore = secretStore;
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
    public OperationResult<string> GetUsername()
    {
        return GetCredentials("username", RuntimeSettings.Username);
    }

    /// <inheritdoc/>
    public OperationResult<string> GetPassword()
    {
        return GetCredentials("password", RuntimeSettings.Password);
    }

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <summary>
    /// Internal helper to retrieve a credential from the secret store, falling back to runtime settings if the store is not set.
    /// </summary>
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

    /// <inheritdoc/>
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