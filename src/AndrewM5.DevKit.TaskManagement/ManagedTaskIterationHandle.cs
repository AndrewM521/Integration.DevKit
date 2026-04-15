using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AndrewM5.DevKit.TaskManagement;

public sealed class ManagedTaskIterationHandle
{
    public int IterationNumber { get; }
    public DateTime StartTime { get; }

    private readonly CancellationTokenSource _cts;

    internal ManagedTaskIterationHandle(int iterationNumber, CancellationToken externalToken)
    {
        IterationNumber = iterationNumber;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);

        StartTime = DateTime.UtcNow;
    }

    public void Cancel() => _cts.Cancel();
    public void Dispose() => _cts.Dispose();
}
