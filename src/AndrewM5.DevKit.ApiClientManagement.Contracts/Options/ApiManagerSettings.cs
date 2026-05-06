/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using AndrewM5.DevKit.ApiClientManagement.Contracts.Interfaces;
using System.Collections.Concurrent;

namespace AndrewM5.DevKit.ApiClientManagement.Contracts.Options;

/// <summary>
/// Represents the configuration for the <see cref="IApiManager"/>.
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
    /// Creates a new instance of <see cref="ApiManagerSettings"/> with a deep copy of the <see cref="Clients"/> collection.
    /// </summary>
    /// <returns>A new <see cref="ApiManagerSettings"/> instance containing a copy of the current client definitions.</returns>
    public ApiManagerSettings Clone()
    {
        return new ApiManagerSettings {
            Default_HttpTimeout_Seconds = this.Default_HttpTimeout_Seconds,
            Clients = new ConcurrentDictionary<string, ApiClientSettings>(this.Clients)
        };
    }
}
