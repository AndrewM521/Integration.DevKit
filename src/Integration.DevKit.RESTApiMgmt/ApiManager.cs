/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Reflection;
using Integration.DevKit.Core.Logging;
using Integration.DevKit.RESTApiMgmt.Settings;

namespace Integration.DevKit.RESTApiMgmt;

/// <summary>
/// Defines a contract for a manager responsible for orchestrating multiple <see cref="ApiClient"/> instances
/// and maintaining global API management configurations.
/// </summary>
public class ApiManager : IAsyncDisposable
{
    /// <summary>
    /// Gets or sets the global configuration settings for the API manager.
    /// </summary>
    /// <value>
    /// An instance of <see cref="ApiManagerSettings"/> containing the current runtime configuration.
    /// </value>
    public ApiManagerSettings RuntimeSettings { get; set; }

    private readonly ConcurrentDictionary<string, ApiClient> _clients = new ConcurrentDictionary<string, ApiClient>(StringComparer.OrdinalIgnoreCase);
    
    private readonly IHttpClientFactory _httpFactory;
    private readonly ILogger? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiManager"/> class.
    /// </summary>
    /// <param name="settings">The initial configuration settings injected via the Options pattern.</param>
    /// <param name="httpFactory">The factory used to create underlying <see cref="HttpClient"/> instances.</param>
    /// <param name="loggerFactory">
    /// Optional factory to resolve the "ApiManager" logger. The resulting logger dynamically honors
    /// <see cref="ApiManagerSettings.EnableLogging"/> on <see cref="RuntimeSettings"/> on every call, so
    /// logging can be toggled at runtime without reconstructing the manager.
    /// </param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="settings"/> is null.</exception>
    public ApiManager(IOptions<ApiManagerSettings> settings, IHttpClientFactory httpFactory, ILoggerFactory? loggerFactory = null)
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

        _logger = loggerFactory?.CreateConditionalLogger("ApiManager", () => RuntimeSettings.EnableLogging);
    }


    /// <summary>
    /// Retrieves a specific API client by its registered name.
    /// </summary>
    /// <param name="clientName">The unique name identifying the desired <see cref="ApiClient"/>.</param>
    /// <returns>
    /// An instance of <see cref="ApiClient"/> associated with the provided <paramref name="clientName"/>.
    /// </returns>
    /// <remarks>
    /// If the requested <paramref name="clientName"/> is not found in the <see cref="ApiManagerSettings.Clients"/>, 
    /// the manager provides a new <see cref="ApiClient"/> using default settings.
    /// </remarks>
    public ApiClient GetClient(string clientName)
    {
        if (!RuntimeSettings.Clients.TryGetValue(clientName, out var clientSettings))
        {
            _logger?.LogWarning($"Api Client '{clientName}' is not configured. Using default settings instead");

            clientSettings = new ApiClientSettings();
        }

        return _clients.GetOrAdd(clientName, _ =>
        {
            /* CRITICAL ARCHITECTURAL NOTE:
               We request a client named "ApiClient" from the factory rather than using the specific 'clientName'.
               This allows .NET to manage the underlying Primary Handler (Layer 2 - DNS, SSL, Connection Pooling) 
               as a single pool, while we manually configure the 'Outer' HttpClient (Layer 1 - BaseUrl, Headers) 
               inside the ApiClient constructor. 
            */
            var httpClientObj = _httpFactory.CreateClient("ApiClient");

            return new ApiClient(this, clientName, clientSettings, httpClientObj, _logger);
        });
    }

    /// <summary>
    /// Logging method to output current <see cref="ApiManagerSettings"/> to the logs.
    /// </summary>
    public void LogRuntimeSettings()
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

                    client.LogRuntimeSettings(true);
                }
            }
            else
            {
                _logger?.LogDebug($"  {property.Name}: {value}");
            }
        }
    }

    /// <summary>
    /// Disposes all cached clients 
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        foreach (var client in _clients.Values)
        {
            await client.DisposeAsync();
        }
    }
}

/// <summary>
/// Defines the specific HTTP methods tracked within the reporting system.
/// </summary>
public enum HttpMetricNames
{
    /// <summary> Represents an HTTP GET operation. </summary>
    Get,
    /// <summary> Represents an HTTP POST operation. </summary>
    Post,
    /// <summary> Represents an HTTP PUT operation. </summary>
    Put,
    /// <summary> Represents an HTTP DELETE operation. </summary>
    Delete,
    /// <summary> Represents an HTTP PATCH operation. </summary>
    Patch,
    /// <summary> Represents an HTTP HEAD operation. </summary>
    Head,
    /// <summary> Represents an HTTP OPTIONS operation. </summary>
    Options,
    /// <summary> Represents any other non-standard or untracked HTTP operation. </summary>
    Other
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
