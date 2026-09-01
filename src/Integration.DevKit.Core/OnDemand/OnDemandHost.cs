using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.DevKit.Core.OnDemand;

/// <summary>
/// A simplified, static host wrapper designed to coordinate service registration, 
/// startup execution, and graceful shutdown in non-DI or on-demand applications.
/// </summary>
public static class OnDemandHost
{
    private static readonly OnDemand_Lifetime _lifetime = new();
    private static IServiceProvider? _services;
    private static bool _isRunning;
    private static readonly SemaphoreSlim _lock = new(1, 1);

    /// <summary>
    /// Gets the compiled service provider used to resolve dependencies.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if accessed before the host is started.</exception>
    public static IServiceProvider Services
    {
        get
        {
            if (_services == null)
            {
                throw new InvalidOperationException("The OnDemandHost has not been started yet. Call OnDemandHost.Start() first.");
            }
            return _services;
        }
    }

    /// <summary>
    /// Configures the on-demand container. Registers core dependencies like the custom lifetime.
    /// </summary>
    public static void ConfigureServices(Action<IServiceCollection> configureAction)
    {
        if (configureAction == null) throw new ArgumentNullException(nameof(configureAction));

        // 1. Ensure our custom lifetime is registered as the application's IHostApplicationLifetime
        if (!OnDemand_Registry.ServiceCollection.Any(d => d.ServiceType == typeof(IHostApplicationLifetime)))
        {
            OnDemand_Registry.ServiceCollection.AddSingleton<IHostApplicationLifetime>(_lifetime);
            OnDemand_Registry.ServiceCollection.AddSingleton(_lifetime); // Also make concrete type resolvable
        }

        // 2. Allow the consumer to easily chain registration actions
        configureAction(OnDemand_Registry.ServiceCollection);
    }

    /// <summary>
    /// Finalizes the service container asynchronously, hooks into OS exit signals, and executes all background hosted services.
    /// </summary>
    public static async Task StartAsync(IConfiguration? configuration = null, CancellationToken cancellationToken = default)
    {
        if (_isRunning) return;

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_isRunning) return;

            // 1. Build the container
            _services = OnDemand_Registry.GetServiceProvider(configuration, forceRebuild: true);

            // 2. Wire up system shutdown hooks automatically (using a fire-and-forget wrapper for sync events)
            AppDomain.CurrentDomain.ProcessExit += (s, e) => Task.Run(StopAsync).GetAwaiter().GetResult();

            var hostedServices = _services.GetServices<IHostedService>();
            var startTasks = new List<Task>();

            foreach (var service in hostedServices)
            {
                // Capture the service loop variable locally to avoid closure issues
                var currentService = service;

                startTasks.Add(Task.Run(() => currentService.StartAsync(_lifetime.ApplicationStopping), cancellationToken));
            }

            await Task.WhenAll(startTasks).ConfigureAwait(false);

            _isRunning = true;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Signals the custom lifetime to gracefully shut down background execution loops asynchronously.
    /// </summary>
    public static async Task StopAsync()
    {
        if (!_isRunning) return;

        await _lock.WaitAsync().ConfigureAwait(false);
        try
        {
            if (!_isRunning) return;

            // 1. Fire the internal cancellation tokens to break the loops (e.g. ExecuteAsync)
            _lifetime.TriggerShutdown();

            // 2. Explicitly invoke StopAsync on all hosted services so they can perform final cleanups
            if (_services != null)
            {
                var hostedServices = _services.GetServices<IHostedService>();
                var stopTasks = new List<Task>();

                foreach (var service in hostedServices)
                {
                    // Capture loop variable locally
                    var currentService = service;
                    stopTasks.Add(Task.Run(() => currentService.StopAsync(CancellationToken.None)));
                }

                await Task.WhenAll(stopTasks).ConfigureAwait(false);
            }

            _isRunning = false;
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <summary>
    /// Finalizes the service container, hooks into OS exit signals, and boots background operations.
    /// </summary>
    public static void Start(IConfiguration? configuration = null)
    {
        if (_isRunning) return;

        // Route directly to the async engine and block until all hosted services are initialized
        Task.Run(async () => await StartAsync(configuration).ConfigureAwait(false)).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Signals the custom lifetime to gracefully shut down background execution loops.
    /// </summary>
    public static void Stop()
    {
        if (!_isRunning) return;

        // Use Task.Run to run the async version and block the thread until it completes.
        // This ensures both the cancellation trigger and all hosted service StopAsync routines execute.
        Task.Run(StopAsync).GetAwaiter().GetResult();
    }
}