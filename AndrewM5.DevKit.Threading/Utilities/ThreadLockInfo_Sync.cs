namespace AndrewM5.DevKit.Threading.Utilities;

internal sealed class ThreadLockInfo_Sync
{
    public object LockObject { get; } = new object();
    public int RefCount = 0;
}
