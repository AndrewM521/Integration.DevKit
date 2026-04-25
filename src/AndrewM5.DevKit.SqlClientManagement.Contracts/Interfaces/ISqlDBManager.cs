using AndrewM5.DevKit.SqlManagement.Abstractions.Options;

namespace AndrewM5.DevKit.SqlManagement.Contracts.Interfaces;

/// <summary>
/// Defines a contract for a manager responsible for the lifecycle, orchestration, and retrieval 
/// of <see cref="ISqlDBClient"/> instances.
/// </summary>
public interface ISqlDBManager : IAsyncDisposable
{
    /// <summary>
    /// Gets or sets the global configuration settings used by the manager 
    /// to orchestrate database clients.
    /// </summary>
    /// <value>
    /// An instance of <see cref="SqlDBManagerSettings"/> containing shared configurations 
    /// such as retry policies, global timeouts, or connection defaults.
    /// </value>
    public SqlDBManagerSettings RuntimeSettings { get; set; }

    /// <summary>
    /// Retrieves a specific SQL database client by its unique name.
    /// </summary>
    /// <param name="clientName">The unique identifier or key of the client to retrieve.</param>
    /// <returns>
    /// An instance of <see cref="ISqlDBClient"/> configured for the specified name.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="clientName"/> is null or empty.</exception>
    public ISqlDBClient GetClient(string clientName);

    /// <summary>
    /// Logging method to output current <see cref="SqlDBManagerSettings"/> to the logs.
    /// </summary>
    public void LogRuntimeSettings();
}
