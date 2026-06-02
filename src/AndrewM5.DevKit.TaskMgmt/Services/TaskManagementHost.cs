/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using AndrewM5.DevKit.TaskMgmt.Contracts.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.TaskMgmt.Services;

/// <summary>
/// Provides a static entry point for accessing core Task Management services.
/// This host must be initialized during application startup before accessing its properties.
/// </summary>
public static class TaskManagementHost
{
    private const string NoInit = "TaskManagementHost has not been initialized.";

    private static ITaskManager? _taskManager;
    private static ITaskRegistry? _taskRegistry;

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
