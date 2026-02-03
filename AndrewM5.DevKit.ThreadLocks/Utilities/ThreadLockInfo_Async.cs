namespace AndrewM5.DevKit.ThreadLocks.Utilities;

internal sealed class ThreadLockInfo_Async : IDisposable
{
    public SemaphoreSlim Semaphore { get; } = new SemaphoreSlim(1, 1);
    public int RefCount = 0;
    public DateTime LastAccessTime = DateTime.MinValue;

    public void Dispose()
    {
        Semaphore.Dispose();
    }
    public void UpdateLastAccessTime()
    {
        LastAccessTime = DateTime.UtcNow;
    }
}
