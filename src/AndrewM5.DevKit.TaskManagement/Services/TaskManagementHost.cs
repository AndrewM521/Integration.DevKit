using AndrewM5.DevKit.TaskManagement.Abstractions.Interfaces;
using AndrewM5.DevKit.ThreadLocks.Contracts.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.TaskManagement.Services;

/// <summary>
/// Provides a static entry point for accessing core Task Management services.
/// This host must be initialized during application startup before accessing its properties.
/// </summary>
public static class TaskManagementHost
{
    private const string NoInit = "TaskManagementHost has not been initialized.";

    private static ITaskManager? _taskManager;
    private static IThreadLockManager? _threadLockManager;
    private static ITaskRegistry? _taskRegistry;

    /// <summary>
    /// Initializes the global service references using the provided service provider.
    /// </summary>
    /// <param name="sp">The <see cref="IServiceProvider"/> used to resolve required services.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="sp"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <see cref="ITaskRegistry"/>, <see cref="ITaskManager"/>, or <see cref="IThreadLockManager"/> 
    /// are not registered in the service collection.
    /// </exception>
    public static void InitializeTaskManagement(IServiceProvider sp)
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

        _threadLockManager = sp.GetService<IThreadLockManager>();
        if (_threadLockManager == null)
        {
            throw new InvalidOperationException($"{nameof(IThreadLockManager)} is not registered. Make sure you call AddTaskManager() when configuring services.");
        }
    }

    /// <summary>
    /// Gets the global <see cref="ITaskManager"/> instance.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if accessed before <see cref="InitializeTaskManagement"/> is called.</exception>
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
    /// Gets the global <see cref="IThreadLockManager"/> instance.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if accessed before <see cref="InitializeTaskManagement"/> is called.</exception>
    public static IThreadLockManager ThreadLockManager
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

    /// <summary>
    /// Gets the global <see cref="ITaskRegistry"/> instance.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if accessed before <see cref="InitializeTaskManagement"/> is called.</exception>
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
