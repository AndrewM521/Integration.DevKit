using AndrewM5.DevKit.Logging.Contracts.Interfaces;
using AndrewM5.DevKit.SqlManagement.Abstractions.Options;
using AndrewM5.DevKit.SqlManagement.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Reflection;

namespace AndrewM5.DevKit.SqlManagement;

/// <summary>
/// Concrete Implementation of <see cref="ISqlDBManager"/>
/// </summary>

public class SqlDBManager : ISqlDBManager
{
    /// <inheritdoc/>
    public SqlDBManagerSettings RuntimeSettings { get; set; }

    private readonly ConcurrentDictionary<string, ISqlDBClient> _clients = new ConcurrentDictionary<string, ISqlDBClient>(StringComparer.OrdinalIgnoreCase);
    private readonly ICustomLogger? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SqlDBManager"/> class.
    /// </summary>
    /// <param name="settings">The injected options containing initial SQL configurations.</param>
    /// <param name="loggerManager">An optional logger manager to provide diagnostic logging.</param>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="settings"/> argument is <see langword="null"/>.</exception>
    internal SqlDBManager(IOptions<SqlDBManagerSettings> settings, ICustomLoggerManager? loggerManager = null)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        RuntimeSettings = settings.Value.Clone();

        _logger = loggerManager?.GetLogger("SqlDBManager");
    }

    /// <inheritdoc/>
    /// <remarks>
    /// This method uses a thread-safe factory approach. If the specified <paramref name="clientName"/> 
    /// is not found in <see cref="RuntimeSettings"/>, a warning is logged and a client is 
    /// generated using an empty <see cref="SqlDBClientSettings"/> instance.
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

    /// <inheritdoc/>
    public void LogRuntimeSettings()
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

                    client.LogRuntimeSettings(true);
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
