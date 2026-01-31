using AndrewM5.DevKit.Threading.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.Threading;

public static class ThreadingHost
{
    private const string NotInitializedMsg = "ThreadingHost has not been initialized.";

    private static IServiceProvider? _serviceProvider;
    private static ITaskManager? _taskManager;
    private static IThreadLockManager? _threadLockManager;
    private static ITaskRegistry? _taskRegistry;

    public static void Initialize(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null)
        {
            throw new ArgumentNullException(nameof(serviceProvider));
        }

        _serviceProvider = serviceProvider;
        _taskManager = _serviceProvider.GetService<ITaskManager>();
        if (_taskManager == null)
        {
            throw new InvalidOperationException($"{nameof(ITaskManager)} is not registered. Make sure you call AddTaskManager() when configuring services before initializing {nameof(ThreadingHost)}.");
        }

        _threadLockManager = _serviceProvider.GetService<IThreadLockManager>();
        if (_threadLockManager == null)
        {
            throw new InvalidOperationException($"{nameof(IThreadLockManager)} is not registered. Make sure you call AddTaskManager() when configuring services before initializing {nameof(ThreadingHost)}.");
        }

        _taskRegistry = _serviceProvider.GetService<ITaskRegistry>();
        if (_taskRegistry == null)
        {
            throw new InvalidOperationException($"{nameof(ITaskRegistry)} is not registered. Make sure you call AddTaskManager() when configuring services before initializing {nameof(ThreadingHost)}.");
        }
    }

    public static IServiceProvider ServiceProvider
    {
        get
        {
            if (_serviceProvider == null)
            {
                throw new InvalidOperationException(NotInitializedMsg);
            }

            return _serviceProvider;
        }
    }

    public static ITaskManager TaskManager
    {
        get
        {
            if (_taskManager == null)
            {
                throw new InvalidOperationException(NotInitializedMsg);
            }

            return _taskManager;
        }
    }

    public static IThreadLockManager ThreadLockManager
    {
        get
        {
            if (_threadLockManager == null)
            {
                throw new InvalidOperationException(NotInitializedMsg);
            }

            return _threadLockManager;
        }
    }

    public static ITaskRegistry TaskRegistry
    {
        get
        {
            if (_taskRegistry == null)
            {
                throw new InvalidOperationException(NotInitializedMsg);
            }

            return _taskRegistry;
        }
    }

    internal static void Reset()
    {
        _serviceProvider = null;
        _taskManager = null;
        _threadLockManager = null;
    }
}
