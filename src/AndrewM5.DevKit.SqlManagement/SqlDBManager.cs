using AndrewM5.DevKit.Logging.Contracts.Interfaces;
using AndrewM5.DevKit.SqlManagement.Abstractions.Options;
using AndrewM5.DevKit.SqlManagement.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Reflection;

namespace AndrewM5.DevKit.SqlManagement;

/// <summary>
/// Provides a concrete implementation of <see cref="ISqlDBManager"/> to manage, 
/// configure, and dispose of multiple <see cref="ISqlDBClient"/> instances.
/// </summary>
public class SqlDBManager : ISqlDBManager
{
    /// <summary>
    /// Gets or sets the cloned runtime configuration for this manager.
    /// </summary>
    public SqlDBManagerSettings RuntimeSettings { get; set; }

    private readonly ConcurrentDictionary<string, ISqlDBClient> _clients = new ConcurrentDictionary<string, ISqlDBClient>(StringComparer.OrdinalIgnoreCase);
    private readonly ICustomLogger? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlDBManager"/> class.
    /// </summary>
    /// <param name="settings">The injected options containing initial SQL configurations.</param>
    /// <param name="loggerManager">An optional logger manager to provide diagnostic logging.</param>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="settings"/> argument is null.</exception>
    public SqlDBManager(IOptions<SqlDBManagerSettings> settings, ICustomLoggerManager? loggerManager = null)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        RuntimeSettings = settings.Value.Clone();

        _logger = loggerManager?.GetLogger("SqlDBManager");
    }

    /// <summary>
    /// Retrieves an existing <see cref="ISqlDBClient"/> or creates a new one if it doesn't exist.
    /// </summary>
    /// <param name="clientName">The name of the client configuration to retrieve.</param>
    /// <returns>A configured <see cref="ISqlDBClient"/> instance.</returns>
    /// <remarks>
    /// If the specified <paramref name="clientName"/> is not found in <see cref="RuntimeSettings"/>, 
    /// a warning is logged and a client is created with default <see cref="SqlDBClientSettings"/>.
    /// </remarks>
    public ISqlDBClient GetClient(string clientName)
    {
        if (!RuntimeSettings.Clients.TryGetValue(clientName, out var clientSettings))
        {
            _logger?.LogWarning($"SqlDB Client '{clientName}' is not configured. Using default settings instead");
            
            clientSettings = new SqlDBClientSettings();
        }

        return _clients.GetOrAdd(clientName, _ => { return new SqlDBClient(this, clientName, clientSettings); });
    }

    /// <summary>
    /// Iterates through all public properties of the <see cref="RuntimeSettings"/> and logs their values.
    /// </summary>
    /// <remarks>
    /// This method uses reflection to discover properties. If a property is a collection of client settings, 
    /// it recursively triggers each client to output its own settings.
    /// </remarks>
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

    /// <summary>
    /// Asynchronously disposes of all managed <see cref="ISqlDBClient"/> instances.
    /// </summary>
    /// <returns>A <see cref="ValueTask"/> representing the completion of the disposal.</returns>
    public ValueTask DisposeAsync()
    {
        foreach (var client in _clients.Values)
        {
            client.Dispose();
        }

        return ValueTask.CompletedTask;
    }
}
