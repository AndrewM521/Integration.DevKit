/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using AndrewM5.DevKit.ProcessLauncher.Contracts.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.ProcessLauncher.Services;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> to register 
/// process launcher services within the dependency injection container.
/// </summary>
public static class ProcessLauncherServiceCollection
{
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
}
