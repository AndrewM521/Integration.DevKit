using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.DevKit.Core;

/// <summary>
/// Provides a centralized, lazily-initialized shared service container 
/// for DevKit modules running in non-DI or on-demand environments.
/// </summary>
public static class OnDemand_Registry
{
    private static readonly IServiceCollection _services = new ServiceCollection();
    private static IServiceProvider? _serviceProvider;
    private static readonly object _lock = new();

    /// <summary>
    /// Gets the shared service collection. Register on-demand dependencies here.
    /// </summary>
    public static IServiceCollection Services => _services;

    /// <summary>
    /// Builds or retrieves the global on-demand service provider.
    /// </summary>
    public static IServiceProvider GetServiceProvider(IConfiguration? configuration = null, bool forceRebuild = false)
    {
        if (_serviceProvider != null && !forceRebuild)
            return _serviceProvider;

        lock (_lock)
        {
            if (_serviceProvider == null || forceRebuild)
            {
                // Optionally inject the configuration into the container if provided
                if (configuration != null && !_services.Any(d => d.ServiceType == typeof(IConfiguration)))
                {
                    _services.AddSingleton(configuration);
                }

                _serviceProvider = _services.BuildServiceProvider();
            }
        }

        return _serviceProvider;
    }
}
