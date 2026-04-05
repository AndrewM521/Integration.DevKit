using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.TaskManagement.Abstractions.Interfaces;
using System.Collections.Concurrent;

namespace AndrewM5.DevKit.TaskManagement;

/// <summary>
/// Orchestrates the synchronization between active task runtimes and the persistent task registry.
/// Handles snapshot creation, state mapping, and registry capacity management.
/// </summary>
internal class TaskRegistryRuntime
{
    internal readonly ITaskRegistry _taskRegistry;

    private readonly ITaskManager _taskManager;

    /// <summary>
    /// Tracks the order in which tasks were added to the registry to facilitate 
    /// First-In-First-Out (FIFO) trimming.
    /// </summary>
    private readonly ConcurrentQueue<string> _insertionOrder = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskRegistryRuntime"/> class.
    /// </summary>
    /// <param name="taskManager">The manager providing global runtime settings.</param>
    /// <param name="taskRegistry">The underlying storage for task snapshots.</param>
    public TaskRegistryRuntime(ITaskManager taskManager, ITaskRegistry taskRegistry)
    {
        _taskRegistry = taskRegistry;
        _taskManager = taskManager;
    }

    /// <summary>
    /// Updates or creates a snapshot in the registry based on the current state of a <see cref="ManagedTaskRuntime"/>.
    /// </summary>
    /// <param name="managedTaskRuntime">The active runtime to capture data from.</param>
    /// <param name="snapshotEx">An optional exception to associate with the snapshot (e.g., if the task just faulted).</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating the success of the update and subsequent trim check.</returns>
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

    /// <summary>
    /// Evaluates the registry size against the <see cref="ManagedTaskSettings.MaxRegistryCount"/> 
    /// and removes the oldest snapshots if the limit is exceeded.
    /// </summary>
    /// <returns>A successful result, or a failure result containing an <see cref="AggregateException"/> if removals fail.</returns>
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
