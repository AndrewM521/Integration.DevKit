namespace AndrewM5.DevKit.ThreadLocks;

internal sealed class ThreadLockInfo_Sync
{
    public object LockObject { get; } = new object();
    public int RefCount = 0;
    public DateTime LastAccessTime = DateTime.MinValue;

    public void UpdateLastAccessTime()
    {
        LastAccessTime = DateTime.UtcNow;
    }
}
