namespace AndrewM5.DevKit.Threading.Utilities;

internal sealed class ThreadLockInfo_Async : IDisposable
{
    public SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(1, 1);
    public int RefCount = 0;

    public void Dispose()
    {
        Semaphore.Dispose();
    }
}
