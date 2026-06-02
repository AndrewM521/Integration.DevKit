/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using System.Collections.Concurrent;

namespace AndrewM5.DevKit.SQLMgmt.Contracts.Options;

/// <summary>
/// Represents the global configuration settings for the SQL Database Manager, 
/// including default behaviors and a collection of managed clients.
/// </summary>
public class SQLManagerSettings
{
    /// <summary>
    /// Gets or sets the thread-safe dictionary of client configurations, 
    /// keyed by their unique client name.
    /// </summary>
    /// <value>
    /// A <see cref="ConcurrentDictionary{TKey, TValue}"/> where the key is the client's unique name 
    /// and the value is its corresponding <see cref="SQLClientSettings"/>.
    /// </value>
    public ConcurrentDictionary<string, SQLClientSettings> Clients { get; set; } = new ConcurrentDictionary<string, SQLClientSettings>();

    /// <summary>
    /// Creates a deep copy of the current settings
    /// </summary>
    /// <returns>A new <see cref="SQLManagerSettings"/> instance independent of the original.</returns>
    public SQLManagerSettings Clone()
    {
        return new SQLManagerSettings
        {
            Clients = new ConcurrentDictionary<string, SQLClientSettings> (this.Clients)
        };
    }
}
