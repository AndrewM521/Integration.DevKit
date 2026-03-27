using AndrewM5.DevKit.ThreadLocks.Contracts.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.ThreadSafeItems.Services;

public static class ThreadSafeItemsHost
{
    private const string NoInit = "ThreadSafeItemsHost has not been initialized.";

    private static ThreadSafeFileIO? _threadSafeFileIO;

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
