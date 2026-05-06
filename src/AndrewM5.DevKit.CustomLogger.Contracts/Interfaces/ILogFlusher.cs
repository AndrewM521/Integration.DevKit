/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using AndrewM5.DevKit.CustomLogger.Contracts.Options;

namespace AndrewM5.DevKit.CustomLogger.Contracts.Interfaces;

/// <summary>
/// Defines a service responsible for managing the flushing of log buffers 
/// to their respective persistent storage or destinations.
/// </summary>
public interface ILogFlusher
{
    /// <summary>
    /// Gets the current operational settings for the log flushing service.
    /// </summary>
    public LogFlushServiceSettings RuntimeSettings { get; }

    /// <summary>
    /// Logging method to output current <see cref="LogFlushServiceSettings"/> to the logs.
    /// </summary>
    public void LogRuntimeSettings();
}
