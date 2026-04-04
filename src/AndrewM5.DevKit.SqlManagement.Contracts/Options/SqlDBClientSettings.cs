namespace AndrewM5.DevKit.SqlManagement.Abstractions.Options;

/// <summary>
/// Represents the configuration and connection settings for a specific SQL Database Client.
/// </summary>
public class SqlDBClientSettings
{
    /// <summary>
    /// Gets or initializes the network address or name of the SQL Server instance.
    /// </summary>
    public string Server { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the name of the primary database to connect to.
    /// </summary>
    public string Database { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the username used for SQL Server authentication.
    /// </summary>
    public string Username { get; init; } = string.Empty;

    /// <summary>
    /// Gets or initializes the password used for SQL Server authentication.
    /// </summary>
    public string Password { get; init; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether Multiple Active Result Sets (MARS) is enabled.
    /// </summary>
    /// <value>Default is <c>false</c>.</value>
    public bool MultipleActiveResultSets { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether the channel will be encrypted while bypassing 
    /// walking the certificate chain to validate trust.
    /// </summary>
    /// <value>Default is <c>true</c>.</value>
    public bool TrustServerCertificate { get; set; } = true;

    /// <summary>
    /// Gets or sets the wait time (in seconds) before terminating the attempt to execute 
    /// a connection and generating an error.
    /// </summary>
    /// <value>Default is <c>15</c> seconds.</value>
    public int ConnectionTimeout_Seconds { get; set; } = 15;

    /// <summary>
    /// Gets or sets the wait time (in seconds) before terminating the attempt to execute 
    /// a command and generating an error.
    /// </summary>
    /// <remarks>
    /// If set to <c>null</c>, the underlying provider's default command timeout is usually used.
    /// </remarks>
    public int? CommandTimeout_Seconds { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the client should persist and reuse a 
    /// single connection instance for all operations.
    /// </summary>
    /// <value>Default is <c>false</c>.</value>
    public bool UseSingleConnection { get; set; } = false;
}