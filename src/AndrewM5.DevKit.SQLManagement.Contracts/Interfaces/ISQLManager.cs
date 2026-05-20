/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using AndrewM5.DevKit.SQLManagement.Contracts.Options;

namespace AndrewM5.DevKit.SQLManagement.Contracts.Interfaces;

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
    /// An instance of <see cref="SQLManagerSettings"/> containing shared configurations 
    /// such as retry policies, global timeouts, or connection defaults.
    /// </value>
    public SQLManagerSettings RuntimeSettings { get; set; }

    /// <summary>
    /// Retrieves a specific SQL database client by its unique name.
    /// </summary>
    /// <param name="clientName">The unique identifier or key of the client to retrieve.</param>
    /// <returns>
    /// An instance of <see cref="ISQLClient"/> configured for the specified name.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown if <paramref name="clientName"/> is null or empty.</exception>
    public ISQLClient GetClient(string clientName);

    /// <summary>
    /// Logging method to output current <see cref="SQLManagerSettings"/> to the logs.
    /// </summary>
    public void LogRuntimeSettings();
}
