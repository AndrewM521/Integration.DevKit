using AndrewM5.DevKit.ThreadLocks.Abstractions;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.ThreadLocks;

public static class ThreadLocksHost
{
    private const string NotInitializedMsg = "ThreadLocksHost has not been initialized.";

    private static IServiceProvider? _serviceProvider;
    private static IThreadLockManager? _threadLockManager;

    public static void Initialize(IServiceProvider serviceProvider)
    {
        if (serviceProvider == null)
        {
            throw new ArgumentNullException(nameof(serviceProvider));
        }

        _serviceProvider = serviceProvider;
        _threadLockManager = _serviceProvider.GetService<IThreadLockManager>();
        if (_threadLockManager == null)
        {
            throw new InvalidOperationException($"{nameof(IThreadLockManager)} is not registered. Make sure you call AddThreadLocks() when configuring services before initializing {nameof(ThreadLocksHost)}.");
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
}
