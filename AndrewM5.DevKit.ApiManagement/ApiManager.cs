using AndrewM5.DevKit.ApiManagement.Abstractions.Settings;
using AndrewM5.DevKit.ApiManagement.Contracts.Interfaces;
using AndrewM5.DevKit.Logging.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Reflection;

namespace AndrewM5.DevKit.ApiManagement;

public class ApiManager : IApiManager
{
    public ApiManagerSettings RuntimeSettings { get; set; }

    private readonly ConcurrentDictionary<string, IApiClient> _clients = new ConcurrentDictionary<string, IApiClient>(StringComparer.OrdinalIgnoreCase);
    
    private readonly IHttpClientFactory _httpFactory;
    private readonly ICustomLogger? _logger;

    public ApiManager(IOptions<ApiManagerSettings> settings, IHttpClientFactory httpFactory, ICustomLoggerManager? loggerManager = null)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        RuntimeSettings = settings.Value.Clone();

        if (RuntimeSettings.Default_HttpTimeout_Seconds < 0)
        {
            RuntimeSettings.Default_HttpTimeout_Seconds = -1;
        }

        _httpFactory = httpFactory;

        _logger = loggerManager?.GetLogger("ApiManager");
    }

    public IApiClient GetClient(string clientName)
    {
        if (!RuntimeSettings.Clients.TryGetValue(clientName, out var clientSettings))
        {
            _logger?.LogWarning($"Api Client '{clientName}' is not configured. Using default settings instead");

            clientSettings = new ApiClientSettings();
        }

        return _clients.GetOrAdd(clientName, _ =>
        {
            /* 
            We do not name the HttpClient object the same as client name because internally HttpClient has 2 layers of configurations
                Layer 1: BaseURL, Timeouts, Tokens, Headers, etc.
                Layer 2: DNS Refresh, SSL options, Proxy Settings, Certificate Settings, etc.
            We can change Layer 1 with the given settings but we should not change Layer 2, as Microsoft warns that multiple instances 
            of Layer 2 will cause performance hits. By using "ApiClient", we are telling .NET to use the same layer 2 config settings 
            for each instances of HttpClient 
            */

            var httpClientObj = _httpFactory.CreateClient("ApiClient");

            return new ApiClient(this, clientName, clientSettings, httpClientObj, _logger);
        });
    }

    public void OutputRuntimeSettings()
    {
        _logger?.LogDebug($"--- Api Manager Settings ---");

        Type type = RuntimeSettings.GetType();
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            object? value = property.GetValue(RuntimeSettings);

            if (value is ConcurrentDictionary<string, ApiClientSettings> dict)
            {
                _logger?.LogDebug($"  {property.Name}:");

                foreach (var kvp in dict)
                {
                    var client = GetClient(kvp.Key);

                    client.OutputRuntimeSettings(true);
                }
            }
            else
            {
                _logger?.LogDebug($"  {property.Name}: {value}");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var client in _clients.Values)
        {
            await client.DisposeAsync();
        }
    }
}
