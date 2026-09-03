using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
using System.Reflection;
using Integration.DevKit.Core.Logging;
using Integration.DevKit.SQLMgmt.Contracts;

namespace Integration.DevKit.SQLMgmt;

/// <summary>
/// Concrete Implementation of <see cref="ISQLManager"/>
/// </summary>

public class SQLManager : ISQLManager
{
    /// <inheritdoc/>
    public SQLManagerSettings RuntimeSettings { get; set; }

    private readonly ConcurrentDictionary<string, ISQLClient> _clients = new ConcurrentDictionary<string, ISQLClient>(StringComparer.OrdinalIgnoreCase);
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

    /// <inheritdoc/>
    /// <remarks>
    /// This method uses a thread-safe factory approach. If the specified <paramref name="clientName"/> 
    /// is not found in <see cref="RuntimeSettings"/>, a warning is logged and a client is 
    /// generated using an empty <see cref="SQLClientSettings"/> instance.
    /// </remarks>
    public ISQLClient GetClient(string clientName)
    {
        if (!RuntimeSettings.Clients.TryGetValue(clientName, out var clientSettings))
        {
            _logger?.LogWarning($"SqlDB Client '{clientName}' is not configured. Using default settings instead");
            
            clientSettings = new SQLClientSettings();
        }

        return _clients.GetOrAdd(clientName, _ => { return new SQLClient(clientName, clientSettings, _logger); });
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
    /// Asynchronously disposes of all managed <see cref="ISQLClient"/> instances.
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
