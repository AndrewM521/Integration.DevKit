namespace AndrewM5.DevKit.SqlManagement.Abstractions.Options;

/// <summary>
/// Represents the configuration and connection settings for a specific SQL Database Client,
/// utilizing a full connection string for database connectivity.
/// </summary>
public class SqlDBClientSettings
{
    /// <summary>
    /// Gets or sets the complete SQL Server connection string used to establish a connection.
    /// </summary>
    /// <remarks>
    /// This string should include all necessary parameters such as Server, Database, 
    /// and Security credentials.
    /// </remarks>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the client should persist and reuse a 
    /// single connection instance for all operations.
    /// </summary>
    /// <value>Default is <c>false</c>.</value>
    public bool UseSingleConnection { get; set; } = false;
}