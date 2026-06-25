/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.CustomLogger.Contracts;
using Integration.DevKit.ProcessLauncher.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.DevKit.ProcessLauncher;

/// <summary>
/// Provides a static entry point to access the Process Launcher module 
/// </summary>
/// <remarks>
/// This acts as a static wrapper for services resolved from the Dependency Injection container. 
/// It must be registered and initialized during application startup (e.g., in Program.cs or Startup.cs)
/// </remarks>
public static class Service_ProcessLauncher
{
    private static readonly IServiceCollection _internalServiceCollection = new ServiceCollection();
    private const string NoInit = "Service_ProcessLauncher has not been initialized.";

    private static IProcessManager? _processManager;

    /// <summary>
    /// Adds the process launcher services to the specified <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown if either <paramref name="services"/> is null.
    /// </exception>
    /// <remarks>
    /// This method registers <see cref="IProcessManager"/> as a singleton, ensuring a 
    /// consistent state for managed processes across the application.
    /// </remarks>
    public static IServiceCollection AddProcessLauncher(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        // Register the concrete class
        services.AddSingleton<IProcessManager, ProcessManager>();

        return services;
    }

    /// <summary>
    /// Initializes the static <see cref="ProcessManager"/>.
    /// </summary>
    /// <param name="sp">The <see cref="IServiceProvider"/> used to resolve <see cref="IProcessManager"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="sp"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <see cref="IProcessManager"/> is not registered in the service collection.
    /// </exception>
    public static void Initialize(IServiceProvider sp)
    {
        if (sp == null)
        {
            throw new ArgumentNullException(nameof(sp));
        }

        _processManager = sp.GetService<IProcessManager>();
        if (_processManager == null)
        {
            throw new InvalidOperationException($"{nameof(IProcessManager)} is not registered. Make sure you call AddProcessLauncher() when configuring services.");
        }
    }


    /// <summary>
    /// Initializes the static <see cref="ProcessManager"/>.
    /// </summary>
    /// <remarks>
    /// This should only be used if your service provider is already built as this adds to an internal service collection. 
    /// </remarks>
    public static void Initialize_OnDemand()
    {
        _internalServiceCollection.AddProcessLauncher();

        var provider = _internalServiceCollection.BuildServiceProvider();

        _processManager = provider.GetRequiredService<IProcessManager>();
    }

    /// <summary>
    /// Gets the global instance of the <see cref="IProcessManager"/>.
    /// </summary>
    /// <value>The initialized <see cref="IProcessManager"/>.</value>
    /// <exception cref="InvalidOperationException">Thrown if accessed before <see cref="Initialize"/> is called.</exception>
    public static IProcessManager ProcessManager
    {
        get
        {
            if (_processManager == null)
            {
                throw new InvalidOperationException(NoInit);
            }

            return _processManager;
        }
    }
}
