/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using AndrewM5.DevKit.CustomLogger.Contracts.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.CustomLogger.Flusher.Services;

/// <summary>
/// Provides a static entry point to access the Logger Flusher module 
/// </summary>
/// <remarks>
/// This host acts as a static wrapper for services resolved from the Dependency Injection container. 
/// It must be initialized during application startup (e.g., in Program.cs or Startup.cs) 
/// after the service provider has been built.
/// </remarks>
public static class LogFlusherHost
{
    private static ILogFlusher? _logFlushService;

    private const string NoInit = "LogFlusherHost has not been initialized.";

    /// <summary>
    /// Initializes the static flusher host and validates that all required logging dependencies are registered.
    /// </summary>
    /// <param name="sp">The <see cref="IServiceProvider"/> used to resolve logging and flushing services.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="sp"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the core Logging module is missing (requires <see cref="ICustomLoggerManager"/> and <see cref="ILogRegistry"/>) 
    /// or if the Flusher service itself has not been registered.
    /// </exception>
    public static void Initalize(IServiceProvider sp)
    {
        if (sp == null)
        {
            throw new ArgumentNullException(nameof(sp));
        }

        try
        {
            _ = sp.GetRequiredService<ICustomLoggerManager>();
            _ = sp.GetRequiredService<ILogRegistry>();
        }
        catch (Exception)
        {
            throw new InvalidOperationException($"{nameof(LogFlusherServiceCollection)} requires the Logging module. Call AddCustomLogging() before AddCustomLogFlusher()");
        }

        _logFlushService = sp.GetService<ILogFlusher>();
        if (_logFlushService == null)
        {
            throw new InvalidOperationException($"{nameof(ILogFlusher)} is not registered, make sure to call AddCustomLogFlusher() when configuring services.");
        }
    }

    /// <summary>
    /// Gets the global instance of the <see cref="ILogFlusher"/> service.
    /// </summary>
    /// <value>The current log flusher instance.</value>
    /// <exception cref="InvalidOperationException">Thrown if accessed before <see cref="Initalize"/> is called.</exception>
    public static ILogFlusher LogFlushService
    {
        get
        {
            if (_logFlushService == null)
            {
                throw new InvalidOperationException(NoInit);
            }

            return _logFlushService;
        }
    }
}
