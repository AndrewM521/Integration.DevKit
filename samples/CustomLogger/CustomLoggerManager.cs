using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Reflection;

namespace CustomLogger;

/// <summary>
/// Provides centralized management for creating and configuring custom logger instances 
/// and accessing global logging runtime settings.
/// </summary>
public class CustomLoggerManager
{
    /// <summary>
    /// Gets the current configuration and operational settings for the logger manager.
    /// </summary>
    /// <value>
    /// A <see cref="LoggerManagerSettings"/> object containing the current global logging configuration.
    /// </value>
    public LoggerManagerSettings RuntimeSettings { get; init; }

    private readonly ConcurrentDictionary<string, CustomLogger> _loggers = new ConcurrentDictionary<string, CustomLogger>(StringComparer.OrdinalIgnoreCase);
    private readonly CustomLogger? _logger;
    private readonly LogFileRegistry _logRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomLoggerManager"/> class.
    /// </summary>
    /// <param name="settings">The configuration settings wrapped in <see cref="IOptions{T}"/>.</param>
    /// <param name="logRegistry">The central registry where log messages will be buffered.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="settings"/> or <paramref name="logRegistry"/> is null.</exception>
    public CustomLoggerManager(IOptions<LoggerManagerSettings> settings, LogFileRegistry logRegistry) 
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        RuntimeSettings = settings.Value.Clone();

        _logRegistry = logRegistry;
        _logger = GetLogger("LoggerManager");
    }


    /// <summary>
    /// Retrieves an existing logger or creates a new one for the specified category name.
    /// </summary>
    /// <param name="categoryName">The name of the category for the logger.</param>
    /// <returns>An instance of <see cref="ICustomLogger"/> associated with the given category.</returns>
    public CustomLogger GetLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, (name) => {
            return new CustomLogger(this, _logRegistry, name);
        });
    }


    /// <summary>
    /// Logging method to output current <see cref="LoggerManagerSettings"/> to the logs.
    /// </summary>
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