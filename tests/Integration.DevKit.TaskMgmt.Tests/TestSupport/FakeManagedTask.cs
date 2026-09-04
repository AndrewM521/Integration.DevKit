using Integration.DevKit.TaskMgmt.Abstractions;
using Integration.DevKit.TaskMgmt.Models;

namespace Integration.DevKit.TaskMgmt.Tests.TestSupport;

/// <summary>
/// A minimal <see cref="ManagedTask"/> whose work is configurable per test: how many times
/// <see cref="DoTaskWork"/> has run so far, and an optional callback controlling delay/throw behavior.
/// </summary>
internal sealed class FakeManagedTask : ManagedTask
{
    public int RunCount;
    public Func<ManagedTaskIterationHandle, Task>? Work { get; set; }

    public FakeManagedTask(string taskName) : base(taskName)
    {
    }

    public override async Task DoTaskWork(ManagedTaskIterationHandle iterationHandle)
    {
        Interlocked.Increment(ref RunCount);

        if (Work != null)
        {
            await Work(iterationHandle);
        }
    }
}
