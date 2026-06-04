/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.RESTApiMgmt.Contracts.Options;

namespace Integration.DevKit.RESTApiMgmt.Contracts.Interfaces;

/// <summary>
/// Defines a contract for a manager responsible for orchestrating multiple <see cref="IApiClient"/> instances 
/// and maintaining global API management configurations.
/// </summary>
public interface IApiManager : IAsyncDisposable
{
    /// <summary>
    /// Gets or sets the global configuration settings for the API manager.
    /// </summary>
    /// <value>
    /// An instance of <see cref="ApiManagerSettings"/> containing the current runtime configuration.
    /// </value>
    public ApiManagerSettings RuntimeSettings { get; set; }

    /// <summary>
    /// Retrieves a specific API client by its registered name.
    /// </summary>
    /// <param name="clientName">The unique name identifying the desired <see cref="IApiClient"/>.</param>
    /// <returns>
    /// An instance of <see cref="IApiClient"/> associated with the provided <paramref name="clientName"/>.
    /// </returns>
    public IApiClient GetClient(string clientName);

    /// <summary>
    /// Logging method to output current <see cref="ApiManagerSettings"/> to the logs.
    /// </summary>
    public void LogRuntimeSettings();
}
