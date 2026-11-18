/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
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

        // Same rule as ApiClientSettings.HttpTimeout_Seconds: negative means no timeout (0). -1 reached HttpClient as -1 seconds, which it rejects.
        if (RuntimeSettings.Default_HttpTimeout_Seconds < 0)
        {
            RuntimeSettings.Default_HttpTimeout_Seconds = 0;
        }

        _httpFactory = httpFactory;

        _logger = loggerFactory?.CreateConditionalLogger(nameof(ApiManager), () => RuntimeSettings.EnableLogging);
    }


    /// <summary>
    /// Retrieves a specific API client by its registered name.
    /// </summary>
    /// <param name="clientName">The unique name identifying the desired <see cref="ApiClient"/>.</param>
    /// <returns>
    /// An instance of <see cref="ApiClient"/> associated with the provided <paramref name="clientName"/>.
    /// </returns>
    /// <remarks>
    /// Names are matched case-insensitively. The client is created on first use and cached.
    /// </remarks>
    /// <exception cref="KeyNotFoundException">Thrown if <paramref name="clientName"/> is not in <see cref="ApiManagerSettings.Clients"/>.</exception>
    public ApiClient GetClient(string clientName)
    {
        if (!RuntimeSettings.Clients.TryGetValue(clientName, out var clientSettings))
        {
            // Never fall back to defaults: the default BaseUrl is a real third-party host, and a typo
            // would send traffic (and any credentials an auth strategy adds) there.
            throw new KeyNotFoundException($"Api client '{clientName}' is not configured. Add it under Integration.DevKit:ApiClientManagement:Clients.");
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
    /// Creates an <see cref="HttpClient"/> with none of an <see cref="ApiClient"/>'s configured base address or
    /// default headers, sharing the same underlying handler pool. Used for auth requests to a different host,
    /// and by <see cref="ApiClient.Initialize"/> to replace a client's <see cref="HttpClient"/> when its base address or timeout changes.
    /// </summary>
    internal HttpClient CreateHeaderFreeHttpClient()
    {
        return _httpFactory.CreateClient("ApiClient");
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
