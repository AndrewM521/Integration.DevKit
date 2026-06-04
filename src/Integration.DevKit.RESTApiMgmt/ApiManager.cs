/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.RESTApiMgmt.Contracts.Interfaces;
using Integration.DevKit.RESTApiMgmt.Contracts.Options;
using Integration.DevKit.CustomLogger.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Reflection;

namespace Integration.DevKit.RESTApiMgmt;

/// <summary>
/// Concrete Implementation of <see cref="IApiManager"/>
/// </summary>
public class ApiManager : IApiManager
{
    /// <inheritdoc/>
    public ApiManagerSettings RuntimeSettings { get; set; }

    private readonly ConcurrentDictionary<string, IApiClient> _clients = new ConcurrentDictionary<string, IApiClient>(StringComparer.OrdinalIgnoreCase);
    
    private readonly IHttpClientFactory _httpFactory;
    private readonly ICustomLogger? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ApiManager"/> class.
    /// </summary>
    /// <param name="settings">The initial configuration settings injected via the Options pattern.</param>
    /// <param name="httpFactory">The factory used to create underlying <see cref="HttpClient"/> instances.</param>
    /// <param name="loggerManager">Optional manager to resolve the "ApiManager" logger.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="settings"/> is null.</exception>
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

    /// <inheritdoc/>
    /// <remarks>
    /// If the requested <paramref name="clientName"/> is not found in the <see cref="ApiManagerSettings.Clients"/>, 
    /// the manager provides a new <see cref="ApiClient"/> using default settings.
    /// </remarks>
    public IApiClient GetClient(string clientName)
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

    /// <inheritdoc/>
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
