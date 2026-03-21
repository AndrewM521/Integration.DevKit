using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.TaskManagement.Abstractions.Interfaces;
using System.Collections.Concurrent;

namespace AndrewM5.DevKit.TaskManagement;

internal class TaskRegistryRuntime
{
    internal readonly ITaskRegistry _taskRegistry;

    private readonly ITaskManager _taskManager;

    private readonly ConcurrentQueue<string> _insertionOrder = new();

    public TaskRegistryRuntime(ITaskManager taskManager, ITaskRegistry taskRegistry)
    {
        _taskRegistry = taskRegistry;
        _taskManager = taskManager;
    }

    public NullOperationResult Upsert(ManagedTaskRuntime managedTaskRuntime, Exception? snapshotEx = null)
    {
        var result = new NullOperationResult();

        try
        {
            ManagedTaskSnapshot snapshot;
            string taskKey = managedTaskRuntime.UserTask.TaskKey;
            if (_taskRegistry.Snapshots.TryGetValue(taskKey, out IManagedTaskSnapshot? existingSnapshot))
            {
                if (existingSnapshot != null)
                {
                    snapshot = (ManagedTaskSnapshot)existingSnapshot;
                }
                else
                {
                    snapshot = new ManagedTaskSnapshot(managedTaskRuntime.UserTask.TaskKey, managedTaskRuntime.RuntimeSettings.Clone());
                }
            }
            else
            {
                snapshot = new ManagedTaskSnapshot(managedTaskRuntime.UserTask.TaskKey, managedTaskRuntime.RuntimeSettings.Clone());
            }

            snapshot.State = managedTaskRuntime.State;
            snapshot.IterationCount = managedTaskRuntime.IterationCount;
            snapshot.StartTime = managedTaskRuntime.StartTime;
            snapshot.EndTime = managedTaskRuntime.EndTime;
            snapshot.Exception = snapshotEx;

            _taskRegistry.Upsert(snapshot);

            TrimIfNeeded();

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    private NullOperationResult TrimIfNeeded()
    {
        var result = new NullOperationResult();
        var errors = new List<Exception>();

        while (_taskRegistry.Snapshots.Count > _taskManager.RuntimeSettings.MaxRegistryCount && _insertionOrder.TryDequeue(out var oldestKey))
        {
            try
            {
                // only remove if it's still the same entry (key could have been reinserted)
                _taskRegistry.Snapshots.TryRemove(oldestKey, out _);
            }
            catch (Exception ex)
            {
                errors.Add(ex);
            }
        }

        if (errors.Count > 0)
        {
            return result.SetMethodFailure(new AggregateException(errors));
        }

        return result.SetMethodSuccess();
    }
}
