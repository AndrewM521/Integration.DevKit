using System.Collections.Concurrent;

namespace AndrewM5.DevKit.SqlManagement.Abstractions.Options;

/// <summary>
/// Represents the global configuration settings for the SQL Database Manager, 
/// including default behaviors and a collection of managed clients.
/// </summary>
public class SqlDBManagerSettings
{
    /// <summary>
    /// Gets or sets the default wait time (in seconds) before terminating a command 
    /// for clients managed by this instance.
    /// </summary>
    /// <value>Default is <c>30</c> seconds.</value>
    public int Default_CommandTimeout_Seconds { get; set; } = 30;

    /// <summary>
    /// Gets or sets the thread-safe dictionary of client configurations, 
    /// keyed by their unique client name.
    /// </summary>
    public ConcurrentDictionary<string, SqlDBClientSettings> Clients { get; set; } = new ConcurrentDictionary<string, SqlDBClientSettings>();

    /// <summary>
    /// Creates a deep copy of the current settings, including a new instance of the 
    /// <see cref="Clients"/> dictionary and a copy of all primitive values.
    /// </summary>
    /// <returns>A new <see cref="SqlDBManagerSettings"/> instance independent of the original.</returns>
    public SqlDBManagerSettings Clone()
    {
        return new SqlDBManagerSettings
        {
            Default_CommandTimeout_Seconds = this.Default_CommandTimeout_Seconds,
            Clients = new ConcurrentDictionary<string, SqlDBClientSettings> (this.Clients)
        };
    }
}
