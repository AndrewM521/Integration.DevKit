using AndrewM5.DevKit.Logging.Settings;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Reflection;

namespace AndrewM5.DevKit.Logging;

public class LoggerManager
{
    private readonly ConcurrentDictionary<string, CustomLogger> _loggers = new ConcurrentDictionary<string, CustomLogger>(StringComparer.OrdinalIgnoreCase);

    public LoggerManagerSettings RuntimeSettings { get; internal set; }

    public LoggerManager(IOptions<LoggerManagerSettings> settings) 
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        RuntimeSettings = settings.Value.Clone();
    }

    public CustomLogger GetLogger(string categoryName)
    {
        return _loggers.GetOrAdd(categoryName, (name) => { 
            return new CustomLogger(this, name); 
        });
    }

    public void DisplayRuntimeSettings()
    {
        Console.WriteLine($"--- Logger Manager Settings ---");

        Type type = RuntimeSettings.GetType();
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            object? value = property.GetValue(RuntimeSettings);
            Console.WriteLine($"  {property.Name}: {value}");
        }
    }
}