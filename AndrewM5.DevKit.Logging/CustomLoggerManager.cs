using AndrewM5.DevKit.Logging.Abstractions.Settings;
using AndrewM5.DevKit.Logging.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Reflection;

namespace AndrewM5.DevKit.Logging;

public class CustomLoggerManager : ICustomLoggerManager
{
    public LoggerManagerSettings RuntimeSettings { get; init; }

    private readonly ConcurrentDictionary<string, CustomLogger> _loggers = new ConcurrentDictionary<string, CustomLogger>(StringComparer.OrdinalIgnoreCase);
    private readonly ICustomLogger? _logger;
    private readonly ILogRegistry _logRegistry;

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

    public ICustomLogger GetLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, (name) => {
            return new CustomLogger(this, _logRegistry, name);
        });
    }

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