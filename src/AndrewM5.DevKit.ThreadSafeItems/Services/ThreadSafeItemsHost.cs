using AndrewM5.DevKit.ThreadLocks.Contracts.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.ThreadSafeItems.Services;

/// <summary>
/// Provides static access to the thread safe instances within the application.
/// This class must be initialized during application startup to resolve the required services.
/// </summary>
/// <remarks>
/// This host ensures that all necessary dependencies, specifically the <see cref="IThreadLockManager"/>,
/// are registered and initialized before providing access to thread-safe I/O operations.
/// </remarks>
public static class ThreadSafeItemsHost
{
    private const string NoInit = "ThreadSafeItemsHost has not been initialized.";

    private static ThreadSafeFileIO? _threadSafeFileIO;

    /// <summary>
    /// Initializes the static host by resolving <see cref="ThreadSafeFileIO"/> from the service provider.
    /// </summary>
    /// <param name="sp">The <see cref="IServiceProvider"/> used to resolve dependencies.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="sp"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the <see cref="IThreadLockManager"/> or <see cref="ThreadSafeFileIO"/> are not registered in the service collection.
    /// </exception>
    public static void Initialize(IServiceProvider sp)
    {
        if (sp == null)
        {
            throw new ArgumentNullException(nameof(sp));
        }

        try
        {
            _ = sp.GetRequiredService<IThreadLockManager>();
        }
        catch (Exception)
        {
            throw new InvalidOperationException($"{nameof(ThreadSafeItemsServiceCollection)} requires the ThreadLocks module. Call AddThreadLocks() before AddThreadSafeItems()");
        }

        _threadSafeFileIO = sp.GetService<ThreadSafeFileIO>();
        if (_threadSafeFileIO == null)
        {
            throw new InvalidOperationException($"{nameof(ThreadSafeFileIO)} is not registered. Make sure you call AddThreadSafeItems() when configuring services.");
        }
    }

    /// <summary>
    /// Gets the globally accessible instance of the <see cref="ThreadSafeFileIO"/> class.
    /// </summary>
    /// <value>
    /// The initialized <see cref="ThreadSafeFileIO"/> instance.
    /// </value>
    /// <exception cref="InvalidOperationException">Thrown if accessed before <see cref="Initialize"/> is called.</exception>
    public static ThreadSafeFileIO ThreadSafeFileIOClass { 
        get
        {
            if (_threadSafeFileIO == null)
            {
                throw new InvalidOperationException(NoInit);
            }

            return _threadSafeFileIO;
        }
    }
}
