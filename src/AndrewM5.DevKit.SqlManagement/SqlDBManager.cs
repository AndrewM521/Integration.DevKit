using AndrewM5.DevKit.Logging.Contracts.Interfaces;
using AndrewM5.DevKit.SqlManagement.Abstractions.Settings;
using AndrewM5.DevKit.SqlManagement.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Reflection;

namespace AndrewM5.DevKit.SqlManagement;

public class SqlDBManager : ISqlDBManager
{
    public SqlDBManagerSettings RuntimeSettings { get; set; }

    private readonly ConcurrentDictionary<string, ISqlDBClient> _clients = new ConcurrentDictionary<string, ISqlDBClient>(StringComparer.OrdinalIgnoreCase);
    private readonly ICustomLogger? _logger;

    public SqlDBManager(IOptions<SqlDBManagerSettings> settings, ICustomLoggerManager? loggerManager = null)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        RuntimeSettings = settings.Value.Clone();

        _logger = loggerManager?.GetLogger("SqlDBManager");
    }

    public ISqlDBClient GetClient(string clientName)
    {
        if (!RuntimeSettings.Clients.TryGetValue(clientName, out var clientSettings))
        {
            _logger?.LogWarning($"SqlDB Client '{clientName}' is not configured. Using default settings instead");
            
            clientSettings = new SqlDBClientSettings();
        }

        return _clients.GetOrAdd(clientName, _ => { return new SqlDBClient(this, clientName, clientSettings); });
    }

    public void OutputRuntimeOptions()
    {
        _logger?.LogDebug($"--- SqlDB Manager Settings ---");

        Type type = RuntimeSettings.GetType();
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            object? value = property.GetValue(RuntimeSettings);

            if (value is ConcurrentDictionary<string, SqlDBClientSettings> dict)
            {
                _logger?.LogDebug($"  {property.Name}:");

                foreach (var kvp in dict)
                {
                    var client = GetClient(kvp.Key);

                    client.OutputRuntimeSettings(true);
                }
            }
            else
            {
                _logger?.LogDebug($"  {property.Name}: {value}");
            }
        }
    }

    public ValueTask DisposeAsync()
    {
        foreach (var client in _clients.Values)
        {
            client.Dispose();
        }

        return ValueTask.CompletedTask;
    }
}
