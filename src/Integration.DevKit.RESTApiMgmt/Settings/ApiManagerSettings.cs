/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using System.Collections.Concurrent;

namespace Integration.DevKit.RESTApiMgmt.Settings;

/// <summary>
/// Represents the configuration for the <see cref="ApiManager"/>.
/// </summary>
public class ApiManagerSettings
{
    /// <summary>
    /// Gets or sets the global default timeout in seconds for HTTP requests.
    /// This value is used when a specific <see cref="ApiClientSettings.HttpTimeout_Seconds"/> is not provided.
    /// Default is 30 seconds.
    /// </summary>
    public int Default_HttpTimeout_Seconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the registry of named <see cref="ApiClientSettings"/>.
    /// </summary>
    /// <value>
    /// A <see cref="ConcurrentDictionary{TKey, TValue}"/> where the key is the client name 
    /// and the value is the corresponding configuration.
    /// </value>
    public ConcurrentDictionary<string, ApiClientSettings> Clients { get; set; } = new ConcurrentDictionary<string, ApiClientSettings>();

    /// <summary>
    /// Gets or sets whether this module logs through the logger factory supplied at registration.
    /// Defaults to <see langword="true"/>. Can be flipped at runtime via <see cref="IApiManager.RuntimeSettings"/>
    /// to silence/resume this module's logging without removing the app's logger.
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Creates a new instance of <see cref="ApiManagerSettings"/> with a deep copy of the <see cref="Clients"/>
    /// collection, preserving <see cref="Default_HttpTimeout_Seconds"/> and <see cref="EnableLogging"/>.
    /// </summary>
    /// <returns>A new <see cref="ApiManagerSettings"/> instance containing a copy of the current client definitions.</returns>
    public ApiManagerSettings Clone()
    {
        return new ApiManagerSettings {
            Default_HttpTimeout_Seconds = Default_HttpTimeout_Seconds,
            Clients = new ConcurrentDictionary<string, ApiClientSettings>(Clients),
            EnableLogging = EnableLogging
        };
    }
}
