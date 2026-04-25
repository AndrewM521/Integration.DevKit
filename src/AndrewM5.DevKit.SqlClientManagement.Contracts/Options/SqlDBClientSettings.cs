namespace AndrewM5.DevKit.SqlManagement.Abstractions.Options;

/// <summary>
/// Represents the configuration and connection settings for a specific <c>ISqlDBClient</c>,
/// utilizing a full connection string for database connectivity and defining connection behavior.
/// </summary>
public class SqlDBClientSettings
{
    /// <summary>
    /// Gets or sets the complete SQL Server connection string used to establish a connection.
    /// </summary>
    /// <value>A string containing the server address, database name, and authentication details.</value>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the client should persist and reuse a 
    /// single connection instance for all operations.
    /// </summary>
    /// <value>
    /// <see langword="true"/> to maintain a single open connection for the lifetime of the client; 
    /// <see langword="false"/> to open and close a connection for each individual operation. 
    /// Default is <see langword="false"/>.
    /// </value>
    public bool UseSingleConnection { get; set; } = false;
}