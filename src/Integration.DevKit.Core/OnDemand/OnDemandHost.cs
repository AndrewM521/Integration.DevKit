using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Integration.DevKit.Core.OnDemand;

/// <summary>
/// A minimal host for applications that want DevKit's DI-based modules without a
/// <c>Microsoft.Extensions.Hosting</c> generic host.
/// </summary>
/// <remarks>
/// Each instance owns its own service collection and lifetime, and can be started and stopped repeatedly.
/// Every start builds a fresh <see cref="IServiceProvider"/> and a fresh <see cref="IHostApplicationLifetime"/>;
/// every stop disposes the provider, so singletons (SQL connections, HTTP clients, ...) are released.
/// </remarks>
public sealed class OnDemandHost : IAsyncDisposable
{
    private readonly IServiceCollection _serviceCollection = new ServiceCollection();
    private readonly SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

    private OnDemand_Lifetime _lifetime = new OnDemand_Lifetime();
    private ServiceProvider? _serviceProvider;
    private bool _isRunning;
    private bool _processExitHooked;

    /// <summary>
    /// Creates a host and registers its <see cref="IHostApplicationLifetime"/>.
    /// </summary>
    public OnDemandHost()
    {
        // Resolved through a factory so each start's new lifetime is the one handed out
        _serviceCollection.AddSingleton<IHostApplicationLifetime>(_ => _lifetime);
    }

    /// <summary>
    /// Gets the service provider built by the current run.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if the host is not running.</exception>
    public IServiceProvider Services
    {
        get
        {
            if (_serviceProvider == null)
            {
                throw new InvalidOperationException("The OnDemandHost is not running. Call StartAsync() first.");
            }

            return _serviceProvider;
        }
    }

    /// <summary>
    /// Gets whether the host is currently started.
    /// </summary>
    public bool IsRunning => _isRunning;

    /// <summary>
    /// Adds services to this host. Changes take effect on the next <see cref="StartAsync"/>.
    /// </summary>
    /// <param name="configureAction">The registration callback.</param>
    /// <returns>This host, for chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="configureAction"/> is <see langword="null"/>.</exception>
    public OnDemandHost ConfigureServices(Action<IServiceCollection> configureAction)
    {
        if (configureAction == null)
        {
            throw new ArgumentNullException(nameof(configureAction));
        }

        configureAction(_serviceCollection);

        return this;
    }

    /// <summary>
    /// Builds the service provider, starts every registered <see cref="IHostedService"/>, then signals
    /// <see cref="IHostApplicationLifetime.ApplicationStarted"/>. Does nothing if the host is already running.
    /// </summary>
    /// <param name="configuration">Optional configuration, registered as <see cref="IConfiguration"/> if none is registered yet.</param>
    /// <param name="cancellationToken">A token to cancel waiting for the start lock.</param>
    public async Task StartAsync(IConfiguration? configuration = null, CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            if (_isRunning)
            {
                return;
            }

            if (configuration != null)
            {
                // Only when the caller didn't register their own IConfiguration already
                bool hasConfiguration = false;

                foreach (var descriptor in _serviceCollection)
                {
                    if (descriptor.ServiceType == typeof(IConfiguration))
                    {
                        hasConfiguration = true;
                        break;
                    }
                }

                if (!hasConfiguration)
                {
                    _serviceCollection.AddSingleton(configuration);
                }
            }

            // A canceled token can't be reset, so every run gets a new lifetime
            _lifetime = new OnDemand_Lifetime();

            // StopApplication() must stop the host, like the generic host. Tied to this run's lifetime so a late
            // callback can't stop a later run; the stop StopAsync triggers itself finds the host already stopped.
            var lifetime = _lifetime;
            lifetime.ApplicationStopping.Register(() => _ = Task.Run(() => StopCoreAsync(lifetime)));

            _serviceProvider = _serviceCollection.BuildServiceProvider();

            try
            {
                // One at a time in registration order, like the generic host: a service may rely on an earlier one having started
                foreach (var service in _serviceProvider.GetServices<IHostedService>())
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    await service.StartAsync(_lifetime.ApplicationStopping).ConfigureAwait(false);
                }
            }
            catch
            {
                // Don't leave a half-started provider behind
                await _serviceProvider.DisposeAsync().ConfigureAwait(false);
                _serviceProvider = null;
                throw;
            }

            if (!_processExitHooked)
            {
                AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
                _processExitHooked = true;
            }

            _isRunning = true;
            _lifetime.NotifyStarted();
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Signals <see cref="IHostApplicationLifetime.ApplicationStopping"/>, stops every hosted service, signals
    /// <see cref="IHostApplicationLifetime.ApplicationStopped"/> and disposes the service provider.
    /// Does nothing if the host is not running.
    /// </summary>
    public Task StopAsync()
    {
        return StopCoreAsync(null);
    }

    /// <summary>
    /// Stops the host. With <paramref name="requestedBy"/>, only stops if that lifetime's run is still the current one.
    /// </summary>
    private async Task StopCoreAsync(OnDemand_Lifetime? requestedBy)
    {
        await _lock.WaitAsync().ConfigureAwait(false);

        try
        {
            if (!_isRunning || _serviceProvider == null || (requestedBy != null && requestedBy != _lifetime))
            {
                return;
            }

            _lifetime.NotifyStopping();

            try
            {
                // Reverse registration order, like the generic host; one failing service doesn't stop the rest from stopping
                List<Exception> errors = new List<Exception>();
                List<IHostedService> services = new List<IHostedService>(_serviceProvider.GetServices<IHostedService>());

                for (int i = services.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        await services[i].StopAsync(CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        errors.Add(ex);
                    }
                }

                if (errors.Count > 0)
                {
                    throw new AggregateException(errors);
                }
            }
            finally
            {
                _lifetime.NotifyStopped();

                await _serviceProvider.DisposeAsync().ConfigureAwait(false);
                _serviceProvider = null;
                _isRunning = false;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Blocking version of <see cref="StartAsync"/>.
    /// </summary>
    public void Start(IConfiguration? configuration = null)
    {
        Task.Run(() => StartAsync(configuration)).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Blocking version of <see cref="StopAsync"/>.
    /// </summary>
    public void Stop()
    {
        Task.Run(StopAsync).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Stops the host and removes its process-exit hook.
    /// </summary>
    public async ValueTask DisposeAsync()
    {
        await StopAsync().ConfigureAwait(false);

        if (_processExitHooked)
        {
            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
            _processExitHooked = false;
        }
    }

    // Fallback so hosted services still stop if the app exits without calling StopAsync
    private void OnProcessExit(object? sender, EventArgs e)
    {
        Task.Run(StopAsync).GetAwaiter().GetResult();
    }
}
