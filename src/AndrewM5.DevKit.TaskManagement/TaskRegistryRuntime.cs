using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.Logging.Contracts.Interfaces;
using AndrewM5.DevKit.TaskManagement.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace AndrewM5.DevKit.TaskManagement;

/// <summary>
/// Orchestrates the synchronization between active task runtimes and the persistent task registry.
/// Handles snapshot creation, state mapping, and registry capacity management.
/// </summary>
internal class TaskRegistryRuntime
{
    private readonly ITaskRegistry _taskRegistry;
    private readonly ITaskManager _taskManager;
    private readonly ICustomLogger? _logger;

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
    public TaskRegistryRuntime(ITaskManager taskManager, ITaskRegistry taskRegistry, ICustomLoggerManager? loggerManager = null)
    {
        _taskRegistry = taskRegistry;
        _taskManager = taskManager;

        _logger = loggerManager?.GetLogger("Managed Task Registry");
    }

    /// <summary>
    /// Updates or creates a snapshot in the registry based on the current state of a <see cref="ManagedTaskRuntime"/>.
    /// </summary>
    /// <param name="managedTaskRuntime">The active runtime to capture data from.</param>
    /// <param name="snapshotEx">An optional exception to associate with the snapshot (e.g., if the task just faulted).</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating the success of the update and subsequent trim check.</returns>
    public NullOperationResult Upsert(ManagedTaskRuntime managedTaskRuntime, ManagedTaskIterationRuntime? iterationRuntime = null, Exception? taskException = null, Exception? iterationException = null)
    {
        var result = new NullOperationResult();

        try
        {
            string taskKey = managedTaskRuntime.UserTask.TaskKey;
            bool isNewTask = !_taskRegistry.Snapshots.ContainsKey(taskKey);

            // 1. Get or Create the Main Task Snapshot
            if (!_taskRegistry.Snapshots.TryGetValue(taskKey, out IManagedTaskSnapshot? existingSnapshot) || existingSnapshot == null)
            {
                existingSnapshot = new ManagedTaskSnapshot(taskKey, managedTaskRuntime.RuntimeSettings.Clone());
            }

            var snapshot = (ManagedTaskSnapshot)existingSnapshot;

            // 2. Update Global Task State
            snapshot.State = managedTaskRuntime.State;
            snapshot.IterationCount = managedTaskRuntime.IterationCount;
            snapshot.StartDTM = managedTaskRuntime.StartDTM;
            snapshot.EndDTM = managedTaskRuntime.EndDTM;
            snapshot.Runtime = managedTaskRuntime.Runtime;
            snapshot.Exception = taskException;

            // 3. Handle Iteration History (The new part)
            if (iterationRuntime != null)
            {
                int iterNum = iterationRuntime.IterationNumber;

                // Check if we already have this iteration recorded
                if (snapshot.IterationHistory.TryGetValue(iterNum, out var existingIter))
                {
                    // Update the existing record (Cast to the concrete class to set properties)
                    var recordToUpdate = (ManagedTaskIterationSnapshot)existingIter;

                    recordToUpdate.State = iterationRuntime.State;
                    recordToUpdate.EndDTM = iterationRuntime.EndDTM;
                    recordToUpdate.Runtime = iterationRuntime.Runtime;
                    recordToUpdate.Exception = iterationException;

                    _logger?.LogDebug("Updated existing iteration {IterNum} for Task: {TaskKey}", iterNum, taskKey);
                }
                else
                {
                    // Create and Add new record
                    var newRecord = new ManagedTaskIterationSnapshot(iterationRuntime, iterationException);
                    newRecord.IterationNumber = iterationRuntime.IterationNumber;
                    newRecord.State = iterationRuntime.State;
                    newRecord.StartDTM = iterationRuntime.StartDTM;
                    newRecord.EndDTM = iterationRuntime.EndDTM;
                    newRecord.Runtime = iterationRuntime.Runtime;
                    newRecord.Exception = iterationException;

                    snapshot.IterationHistory.Add(iterNum, newRecord);

                    // 4. Efficient Internal Trimming
                    int maxIterations = _taskManager.RuntimeSettings.MaxTaskIterationRegistryCount;
                    if (snapshot.IterationHistory.Count > maxIterations)
                    {
                        _logger?.LogDebug($"Trimming iteration history for Task: {taskKey}. Current count: {snapshot.IterationHistory.Count}, Max allowed: {maxIterations}");

                        // SortedDictionary keeps keys in order, so the first key is the oldest iteration
                        while (snapshot.IterationHistory.Count > maxIterations)
                        {
                            var oldestIterKey = snapshot.IterationHistory.Keys.First();
                            snapshot.IterationHistory.Remove(oldestIterKey);
                        }
                    }
                }
            }

            // 4. Save to Registry
            _taskRegistry.Upsert(snapshot);

            // 5. Global Registry Trimming
            // If this was a new task, track its key for the FIFO eviction policy
            if (isNewTask)
            {
                _insertionOrder.Enqueue(taskKey);
            }

            int taskHistoryCount = _taskRegistry.Snapshots.Count;
            int maxTaskHistory = _taskManager.RuntimeSettings.MaxTaskRegistryCount;
            while (taskHistoryCount > maxTaskHistory && _insertionOrder.TryDequeue(out var oldestKey))
            {
                try
                {
                    _logger?.LogDebug($"Registry limit exceeded ({maxTaskHistory}). Removing oldest task snapshot: {oldestKey}");

                    if (!_taskRegistry.Snapshots.TryRemove(oldestKey, out _))
                    {
                        _logger?.LogWarning($"Failed to remove task snapshot for key: {oldestKey}. It may have already been removed.");
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError($"Error while removing task {oldestKey} from registry. {ex.Message}");
                }
            }

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }
}
