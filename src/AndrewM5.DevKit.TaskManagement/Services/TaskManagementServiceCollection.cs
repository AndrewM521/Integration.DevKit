/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using AndrewM5.DevKit.TaskManagement.Contracts.Interfaces;
using AndrewM5.DevKit.TaskManagement.Contracts.Options;
using AndrewM5.DevKit.ThreadLocks;
using AndrewM5.DevKit.ThreadLocks.Contracts.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.TaskManagement.Services;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> to simplify the registration of Task Management services.
/// </summary>
public static class TaskManagementServiceCollection
{
    /// <summary>
    /// Registers the core Task Management services, including the Task Manager, Registry, and Thread Lock Manager.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="config">The <see cref="IConfiguration"/> instance used to bind <see cref="TaskManagerSettings"/>.</param>
    /// <returns>The original <see cref="IServiceCollection"/> for chaining calls.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="services"/> or <paramref name="config"/> is null.</exception>
    /// <remarks>
    /// This method binds configuration from the "AndrewM5.DevKit:TaskManager" section and registers 
    /// <see cref="ITaskRegistry"/>, <see cref="ITaskManager"/>, and <see cref="IThreadLockManager"/> as Singletons.
    /// </remarks>
    public static IServiceCollection AddTaskManagement(this IServiceCollection services, IConfiguration config)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }
        
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        // Bind settings
        services.Configure<TaskManagerSettings>(config.GetSection("AndrewM5.DevKit:TaskManagement"));

        // Register Task Registry (holds snapshots/history of tasks)
        services.AddSingleton<ITaskRegistry, TaskRegistry>();

        // Register TaskManager as singleton
        services.AddSingleton<ITaskManager, TaskManager>();

        return services;
    }
}
