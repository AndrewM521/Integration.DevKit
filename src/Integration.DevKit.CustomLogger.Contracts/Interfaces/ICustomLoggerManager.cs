/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

namespace Integration.DevKit.CustomLogger.Contracts;

/// <summary>
/// Provides centralized management for creating and configuring custom logger instances 
/// and accessing global logging runtime settings.
/// </summary>
public interface ICustomLoggerManager
{
    /// <summary>
    /// Gets the current configuration and operational settings for the logger manager.
    /// </summary>
    /// <value>
    /// A <see cref="LoggerManagerSettings"/> object containing the current global logging configuration.
    /// </value>
    public LoggerManagerSettings RuntimeSettings { get; }

    /// <summary>
    /// Retrieves an existing logger or creates a new one for the specified category name.
    /// </summary>
    /// <param name="categoryName">The name of the category for the logger.</param>
    /// <returns>An instance of <see cref="ICustomLogger"/> associated with the given category.</returns>
    public ICustomLogger GetLogger(string categoryName);

    /// <summary>
    /// Logging method to output current <see cref="LoggerManagerSettings"/> to the logs.
    /// </summary>
    public void LogRuntimeSettings();
}
