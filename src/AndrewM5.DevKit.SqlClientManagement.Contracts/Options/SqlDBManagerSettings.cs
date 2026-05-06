/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using System.Collections.Concurrent;

namespace AndrewM5.DevKit.SqlManagement.Abstractions.Options;

/// <summary>
/// Represents the global configuration settings for the SQL Database Manager, 
/// including default behaviors and a collection of managed clients.
/// </summary>
public class SqlDBManagerSettings
{
    /// <summary>
    /// Gets or sets the thread-safe dictionary of client configurations, 
    /// keyed by their unique client name.
    /// </summary>
    /// <value>
    /// A <see cref="ConcurrentDictionary{TKey, TValue}"/> where the key is the client's unique name 
    /// and the value is its corresponding <see cref="SqlDBClientSettings"/>.
    /// </value>
    public ConcurrentDictionary<string, SqlDBClientSettings> Clients { get; set; } = new ConcurrentDictionary<string, SqlDBClientSettings>();

    /// <summary>
    /// Creates a deep copy of the current settings
    /// </summary>
    /// <returns>A new <see cref="SqlDBManagerSettings"/> instance independent of the original.</returns>
    public SqlDBManagerSettings Clone()
    {
        return new SqlDBManagerSettings
        {
            Clients = new ConcurrentDictionary<string, SqlDBClientSettings> (this.Clients)
        };
    }
}
