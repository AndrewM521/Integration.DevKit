/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.ThreadLocks.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Integration.DevKit.ThreadLocks;

/// <summary>
/// Provides a static entry point for accessing the <see cref="ThreadLockManager"/> instance.
/// </summary>
/// <remarks>
/// This host acts as a static wrapper for services resolved from the Dependency Injection container.
/// It must be initialized during application startup (e.g., in Program.cs or Startup.cs)
/// after the service provider has been built.
/// </remarks>
public static class Service_ThreadLocks
{
    private const string NoInit = "Service_ThreadLocks has not been initialized.";

    private static ThreadLockManager? _threadLockManager;

    /// <summary>
    /// Registers the <see cref="ThreadLockManager"/> as a singleton service.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="config">The application configuration used to bind <see cref="ThreadLockSettings"/>.</param>
    /// <returns>
    /// The same <see cref="IServiceCollection"/> instance so that multiple calls can be chained.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="services"/> or <paramref name="config"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddThreadLocks(this IServiceCollection services, IConfiguration config)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        services.Configure<ThreadLockSettings>(config.GetSection("Integration.DevKit:ThreadLocks"));

        // Optionally, also register ThreadLockManager if you want it auto-resolved
        services.TryAddSingleton<ThreadLockManager>();

        return services;
    }

    /// <summary>
    /// Initializes the static host with the required <see cref="ThreadLockManager"/> from the service provider.
    /// </summary>
    /// <param name="sp">The <see cref="IServiceProvider"/> used to resolve the manager.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="sp"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <see cref="ThreadLockManager"/> has not been registered in the service collection.
    /// </exception>
    public static void Initialize(IServiceProvider sp)
    {
        if (sp == null)
        {
            throw new ArgumentNullException(nameof(sp));
        }

        _threadLockManager = sp.GetService<ThreadLockManager>();
        if (_threadLockManager == null)
        {
            throw new InvalidOperationException($"{nameof(ThreadLockManager)} is not registered. Make sure you call AddThreadLocks() when configuring services before initializing {nameof(Service_ThreadLocks)}.");
        }
    }

    /// <summary>
    /// Gets the globally accessible instance of the <see cref="ThreadLockManager"/>.
    /// </summary>
    /// <value>
    /// The current <see cref="ThreadLockManager"/> resolved during initialization.
    /// </value>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the property is accessed before <see cref="Initialize(IServiceProvider)"/> has been called.
    /// </exception>
    public static ThreadLockManager ThreadLockManager
    {
        get
        {
            if (_threadLockManager == null)
            {
                throw new InvalidOperationException(NoInit);
            }

            return _threadLockManager;
        }
    }
}
