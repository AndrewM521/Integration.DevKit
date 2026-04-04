using AndrewM5.DevKit.SqlManagement.Abstractions.Options;

namespace AndrewM5.DevKit.SqlManagement.Contracts.Interfaces;

/// <summary>
/// Defines a contract for a manager responsible for the lifecycle and retrieval 
/// of <see cref="ISqlDBClient"/> instances.
/// </summary>
public interface ISqlDBManager : IAsyncDisposable
{
    /// <summary>
    /// Gets or sets the global configuration settings used by the manager 
    /// to orchestrate database clients.
    /// </summary>
    public SqlDBManagerSettings RuntimeSettings { get; set; }

    /// <summary>
    /// Retrieves a specific SQL database client by its name.
    /// </summary>
    /// <param name="clientName">The unique identifier of the client to retrieve.</param>
    /// <returns>An instance of <see cref="ISqlDBClient"/> configured for the specified name.</returns>
    /// <remarks>
    /// This method typically handles the instantiation or lookup of clients based on 
    /// the provided <paramref name="clientName"/>.
    /// </remarks>
    public ISqlDBClient GetClient(string clientName);

    /// <summary>
    /// Outputs the current management-level runtime options to the configured output or log.
    /// </summary>
    public void OutputRuntimeOptions();
}
