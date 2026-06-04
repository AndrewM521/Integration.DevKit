/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.CustomLogger.Contracts.Interfaces;
using Integration.DevKit.CustomLogger.Contracts.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Reflection;

namespace Integration.DevKit.CustomLogger;

/// <summary>
/// Concrete Implementation of <see cref="ICustomLoggerManager"/>
/// </summary>
public class CustomLoggerManager : ICustomLoggerManager
{
    /// <inheritdoc />
    public LoggerManagerSettings RuntimeSettings { get; init; }

    private readonly ConcurrentDictionary<string, CustomLogger> _loggers = new ConcurrentDictionary<string, CustomLogger>(StringComparer.OrdinalIgnoreCase);
    private readonly ICustomLogger? _logger;
    private readonly ILogRegistry _logRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomLoggerManager"/> class.
    /// </summary>
    /// <param name="settings">The configuration settings wrapped in <see cref="IOptions{T}"/>.</param>
    /// <param name="logRegistry">The central registry where log messages will be buffered.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="settings"/> or <paramref name="logRegistry"/> is null.</exception>
    public CustomLoggerManager(IOptions<LoggerManagerSettings> settings, ILogRegistry logRegistry) 
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        RuntimeSettings = settings.Value.Clone();

        _logRegistry = logRegistry;
        _logger = GetLogger("LoggerManager");
    }

    /// <inheritdoc />
    public ICustomLogger GetLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, (name) => {
            return new CustomLogger(this, _logRegistry, name);
        });
    }

    /// <inheritdoc/>
    public void LogRuntimeSettings()
    {
        _logger?.LogDebug($"--- Custom Logger Manager Settings ---");

        Type type = RuntimeSettings.GetType();
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            object? value = property.GetValue(RuntimeSettings);
            _logger?.LogDebug($"  {property.Name}: {value}");
        }
    }
}