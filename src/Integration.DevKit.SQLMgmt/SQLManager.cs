using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Reflection;
using Integration.DevKit.Core.Logging;
using Integration.DevKit.SQLMgmt.Settings;

namespace Integration.DevKit.SQLMgmt;

/// <summary>
/// Manager responsible for the lifecycle, orchestration, and retrieval
/// of <see cref="SQLClient"/> instances.
/// </summary>

public class SQLManager : IAsyncDisposable
{
    /// <summary>
    /// Gets or sets the global configuration settings used by the manager
    /// to orchestrate database clients.
    /// </summary>
    /// <value>
    /// An instance of <see cref="SQLManagerSettings"/> containing the configured SQL clients and the
    /// module's logging toggle.
    /// </value>
    public SQLManagerSettings RuntimeSettings { get; set; }

    private readonly ConcurrentDictionary<string, SQLClient> _clients = new ConcurrentDictionary<string, SQLClient>(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SQLManager"/> class.
    /// </summary>
    /// <param name="settings">The injected options containing initial SQL configurations.</param>
    /// <param name="loggerFactory">An optional logger factory to provide diagnostic logging.</param>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="settings"/> argument is <see langword="null"/>.</exception>
    public SQLManager(IOptions<SQLManagerSettings> settings, ILoggerFactory? loggerFactory = null)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        RuntimeSettings = settings.Value.Clone();

        _logger = loggerFactory?.CreateConditionalLogger("SqlDBManager", () => RuntimeSettings.EnableLogging);
    }

    /// <summary>
    /// Retrieves a specific SQL database client by its unique name.
    /// </summary>
    /// <param name="clientName">The unique identifier or key of the client to retrieve.</param>
    /// <returns>
    /// An instance of <see cref="SQLClient"/> configured for the specified name. If
    /// <paramref name="clientName"/> is not found in <see cref="RuntimeSettings"/>, a warning is
    /// logged and a client backed by default settings is returned instead — this method does not throw.
    /// </returns>
    /// <remarks>
    /// This method uses a thread-safe factory approach. If the specified <paramref name="clientName"/>
    /// is not found in <see cref="RuntimeSettings"/>, a warning is logged and a client is
    /// generated using an empty <see cref="SQLClientSettings"/> instance.
    /// </remarks>
    public SQLClient GetClient(string clientName)
    {
        if (!RuntimeSettings.Clients.TryGetValue(clientName, out var clientSettings))
        {
            _logger?.LogWarning($"SqlDB Client '{clientName}' is not configured. Using default settings instead");
            
            clientSettings = new SQLClientSettings();
        }

        return _clients.GetOrAdd(clientName, _ => { return new SQLClient(clientName, clientSettings, _logger); });
    }

    /// <summary>
    /// Logging method to output current <see cref="SQLManagerSettings"/> to the logs.
    /// </summary>
    public void LogRuntimeSettings()
    {
        _logger?.LogDebug($"--- SqlDB Manager Settings ---");

        Type type = RuntimeSettings.GetType();
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            object? value = property.GetValue(RuntimeSettings);

            if (value is ConcurrentDictionary<string, SQLClientSettings> dict)
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
    /// Asynchronously disposes of all managed <see cref="SQLClient"/> instances.
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
