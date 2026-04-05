using AndrewM5.DevKit.ThreadLocks.Contracts.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.ThreadLocks.Services;

/// <summary>
/// Provides static access to the <see cref="IThreadLockManager"/> instance within the application.
/// This class must be initialized during application startup to resolve the required services.
/// </summary>
public static class ThreadLocksHost
{
    private const string NoInit = "ThreadLocksHost has not been initialized.";

    private static IThreadLockManager? _threadLockManager;

    /// <summary>
    /// Initializes the static host with the required <see cref="IThreadLockManager"/> from the service provider.
    /// </summary>
    /// <param name="sp">The <see cref="IServiceProvider"/> used to resolve the manager.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="sp"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <see cref="IThreadLockManager"/> has not been registered in the service collection.
    /// </exception>
    public static void Initialize(IServiceProvider sp)
    {
        if (sp == null)
        {
            throw new ArgumentNullException(nameof(sp));
        }

        _threadLockManager = sp.GetService<IThreadLockManager>();
        if (_threadLockManager == null)
        {
            throw new InvalidOperationException($"{nameof(IThreadLockManager)} is not registered. Make sure you call AddThreadLocks() when configuring services before initializing {nameof(ThreadLocksHost)}.");
        }
    }

    /// <summary>
    /// Gets the globally accessible instance of the <see cref="IThreadLockManager"/>.
    /// </summary>
    /// <value>
    /// The initialized <see cref="IThreadLockManager"/> instance.
    /// </value>
    /// <exception cref="InvalidOperationException">Thrown if accessed before <see cref="Initialize"/> is called.</exception>
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
}
