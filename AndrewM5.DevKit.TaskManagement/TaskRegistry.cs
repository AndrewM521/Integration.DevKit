using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.TaskManagement.Abstractions.Interfaces;
using System.Collections.Concurrent;

namespace AndrewM5.DevKit.TaskManagement;

internal class TaskRegistry : ITaskRegistry
{
    public ConcurrentDictionary<string, IManagedTaskSnapshot> Snapshots { get; private set; }

    public TaskRegistry() {
        Snapshots = new ConcurrentDictionary<string, IManagedTaskSnapshot>();
    }

    public NullableOperationResult<IManagedTaskSnapshot?> TryGet(string taskKey)
    {
        var result = new NullableOperationResult<IManagedTaskSnapshot?>();

        try
        {
            Snapshots.TryGetValue(taskKey, out IManagedTaskSnapshot? snapshot);

            return result.SetMethodSuccess(snapshot);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }   
    }

    public NullOperationResult Upsert(IManagedTaskSnapshot snapshot)
    {
        var result = new NullOperationResult();

        try
        {
            Snapshots[snapshot.TaskKey] = snapshot;

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    public NullOperationResult Remove(string taskKey)
    {
        var result = new NullOperationResult();

        try
        {
            Snapshots.TryRemove(taskKey, out _);

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }
}
