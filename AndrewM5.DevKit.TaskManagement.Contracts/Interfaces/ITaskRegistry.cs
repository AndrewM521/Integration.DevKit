using AndrewM5.DevKit.Core.Results;
using System.Collections.Concurrent;

namespace AndrewM5.DevKit.TaskManagement.Abstractions.Interfaces;

public interface ITaskRegistry
{
    public ConcurrentDictionary<string, IManagedTaskSnapshot> Snapshots { get; }

    public NullOperationResult Upsert(IManagedTaskSnapshot snapshot);

    public NullableOperationResult<IManagedTaskSnapshot?> TryGet(string taskKey);

    public NullOperationResult Remove(string taskKey);
}
