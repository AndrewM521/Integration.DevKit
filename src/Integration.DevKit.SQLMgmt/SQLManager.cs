using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Collections.Concurrent;
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

        _logger = loggerFactory?.CreateConditionalLogger(nameof(SQLManager), () => RuntimeSettings.EnableLogging);
    }

    /// <summary>
    /// Retrieves a specific SQL database client by its unique name.
    /// </summary>
    /// <param name="clientName">The unique identifier or key of the client to retrieve.</param>
    /// <returns>
    /// An instance of <see cref="SQLClient"/> configured for the specified name (matched case-insensitively).
    /// </returns>
    /// <remarks>
    /// This method uses a thread-safe factory approach: the client is created on first use and cached.
    /// </remarks>
    /// <exception cref="KeyNotFoundException">Thrown if <paramref name="clientName"/> is not in <see cref="SQLManagerSettings.Clients"/>.</exception>
    public SQLClient GetClient(string clientName)
    {
        if (!RuntimeSettings.Clients.TryGetValue(clientName, out var clientSettings))
        {
            // Never fall back to defaults: a misspelled name would otherwise be cached with empty settings
            throw new KeyNotFoundException($"SQL client '{clientName}' is not configured. Add it under Integration.DevKit:SQLManagement:Clients.");
        }

        return _clients.GetOrAdd(clientName, _ => { return new SQLClient(clientName, clientSettings, _logger); });
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
