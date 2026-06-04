/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.TaskMgmt.Contracts.Interfaces;
using Integration.DevKit.TaskMgmt.Contracts.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.DevKit.TaskMgmt;

/// <summary>
/// Provides a static entry point for accessing core Task Management services.
/// This host must be initialized during application startup before accessing its properties.
/// </summary>
public static class Service_TaskMgmt
{
    private const string NoInit = "Service_TaskMgmt has not been initialized.";

    private static ITaskManager? _taskManager;
    private static ITaskRegistry? _taskRegistry;

    /// <summary>
    /// Registers the core Task Management services, including the Task Manager, Registry, and Thread Lock Manager.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="config">The <see cref="IConfiguration"/> instance used to bind <see cref="TaskManagerSettings"/>.</param>
    /// <returns>The original <see cref="IServiceCollection"/> for chaining calls.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="services"/> or <paramref name="config"/> is null.</exception>
    /// <remarks>
    /// This method binds configuration from the "AndrewM5.DevKit:TaskManager" section and registers 
    /// <see cref="ITaskRegistry"/>, and <see cref="ITaskManager"/> as Singletons.
    /// </remarks>
    public static IServiceCollection AddTaskMgmt(this IServiceCollection services, IConfiguration config)
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
        services.Configure<TaskManagerSettings>(config.GetSection("Integration.DevKit:TaskManagement"));

        // Register Task Registry (holds snapshots/history of tasks)
        services.AddSingleton<ITaskRegistry, TaskRegistry>();

        // Register TaskManager as singleton
        services.AddSingleton<ITaskManager, TaskManager>();

        return services;
    }

    /// <summary>
    /// Initializes the global service references using the provided service provider.
    /// </summary>
    /// <param name="sp">The <see cref="IServiceProvider"/> used to resolve required services.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="sp"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <see cref="ITaskRegistry"/> or <see cref="ITaskManager"/> are not registered in the service collection.
    /// </exception>
    public static void Initialize(IServiceProvider sp)
    {
        if (sp == null)
        {
            throw new ArgumentNullException(nameof(sp));
        }

        _taskRegistry = sp.GetService<ITaskRegistry>();
        if (_taskRegistry == null)
        {
            throw new InvalidOperationException($"{nameof(ITaskRegistry)} is not registered. Make sure you call AddTaskManager() when configuring services.");
        }

        _taskManager = sp.GetService<ITaskManager>();
        if (_taskManager == null)
        {
            throw new InvalidOperationException($"{nameof(ITaskManager)} is not registered. Make sure you call AddTaskManager() when configuring services.");
        }
    }

    /// <summary>
    /// Gets the global <see cref="ITaskManager"/> instance.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if accessed before <see cref="Initialize"/> is called.</exception>
    public static ITaskManager TaskManager
    {
        get
        {
            if (_taskManager == null)
            {
                throw new InvalidOperationException(NoInit);
            }

            return _taskManager;
        }
    }

    /// <summary>
    /// Gets the global <see cref="ITaskRegistry"/> instance.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if accessed before <see cref="Initialize"/> is called.</exception>
    public static ITaskRegistry TaskRegistry
    {
        get
        {
            if (_taskRegistry == null)
            {
                throw new InvalidOperationException(NoInit);
            }

            return _taskRegistry;
        }
    }
}
