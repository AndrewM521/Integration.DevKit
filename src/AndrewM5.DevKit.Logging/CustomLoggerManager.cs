using AndrewM5.DevKit.Logging.Abstractions.Options;
using AndrewM5.DevKit.Logging.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Reflection;

namespace AndrewM5.DevKit.Logging;

/// <summary>
/// Implements the <see cref="ICustomLoggerManager"/> interface to coordinate the creation 
/// of <see cref="CustomLogger"/> instances and manage global logging configuration.
/// </summary>
public class CustomLoggerManager : ICustomLoggerManager
{
    /// <inheritdoc />
    /// <remarks>
    /// This property is initialized as a cloned copy of the provided options to prevent 
    /// external mutations from affecting the initial configuration unexpectedly.
    /// </remarks>
    public LoggerManagerSettings RuntimeSettings { get; init; }

    private readonly ConcurrentDictionary<string, CustomLogger> _loggers = new ConcurrentDictionary<string, CustomLogger>(StringComparer.OrdinalIgnoreCase);
    private readonly ICustomLogger? _logger;
    private readonly ILogRegistry _logRegistry;

    /// <summary>
    /// Initializes a new instance of the <see cref="CustomLoggerManager"/> class.
    /// </summary>
    /// <param name="settings">The configuration settings wrapped in <see cref="IOptions{T}"/>.</param>
    /// <param name="logRegistry">The central registry where log messages will be buffered.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="settings"/> is null.</exception>
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
    /// <remarks>
    /// This method is thread-safe and ensures that only one logger instance is created per 
    /// unique category name (case-insensitive).
    /// </remarks>
    public ICustomLogger GetLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, (name) => {
            return new CustomLogger(this, _logRegistry, name);
        });
    }

    /// <inheritdoc />
    /// <remarks>
    /// This method uses reflection to iterate through the public properties of 
    /// <see cref="RuntimeSettings"/> and logs their current values at the Debug level.
    /// </remarks>
    public void OutputRuntimeSettings()
    {
        _logger?.LogDebug($"--- Logger Manager Settings ---");

        Type type = RuntimeSettings.GetType();
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            object? value = property.GetValue(RuntimeSettings);
            _logger?.LogDebug($"  {property.Name}: {value}");
        }
    }
}