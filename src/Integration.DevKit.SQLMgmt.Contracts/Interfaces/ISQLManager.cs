/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

namespace Integration.DevKit.SQLMgmt.Contracts;

/// <summary>
/// Defines a contract for a manager responsible for the lifecycle, orchestration, and retrieval 
/// of <see cref="ISQLClient"/> instances.
/// </summary>
public interface ISQLManager : IAsyncDisposable
{
    /// <summary>
    /// Gets or sets the global configuration settings used by the manager 
    /// to orchestrate database clients.
    /// </summary>
    /// <value>
    /// An instance of <see cref="SQLManagerSettings"/> containing the configured SQL clients and the
    /// module's logging toggle.
    /// </value>
    public SQLManagerSettings RuntimeSettings { get; set; }

    /// <summary>
    /// Retrieves a specific SQL database client by its unique name.
    /// </summary>
    /// <param name="clientName">The unique identifier or key of the client to retrieve.</param>
    /// <returns>
    /// An instance of <see cref="ISQLClient"/> configured for the specified name. If
    /// <paramref name="clientName"/> is not found in <see cref="RuntimeSettings"/>, a warning is
    /// logged and a client backed by default settings is returned instead — this method does not throw.
    /// </returns>
    public ISQLClient GetClient(string clientName);

    /// <summary>
    /// Logging method to output current <see cref="SQLManagerSettings"/> to the logs.
    /// </summary>
    public void LogRuntimeSettings();
}
