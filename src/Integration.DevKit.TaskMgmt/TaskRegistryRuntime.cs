/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.Core.Results;
using Integration.DevKit.CustomLogger.Contracts.Interfaces;
using Integration.DevKit.TaskMgmt.Contracts.Interfaces;
using Integration.DevKit.TaskMgmt.Contracts.Options;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace Integration.DevKit.TaskMgmt;

/// <summary>
/// Orchestrates the synchronization between active task runtimes and the persistent task registry.
/// </summary>
/// <remarks>
/// This class handles the conversion of live runtime data into static snapshots. It also implements 
/// capacity management policies, ensuring that neither the global registry nor individual task histories 
/// exceed the limits defined in <see cref="TaskManagerSettings"/>.
/// </remarks>
internal class TaskRegistryRuntime
{
    private readonly ITaskRegistry _taskRegistry;
    private readonly ITaskManager _taskManager;
    private readonly ICustomLogger? _logger;

    /// <summary>
    /// Tracks the order in which tasks were added to the registry to facilitate 
    /// First-In-First-Out (FIFO) trimming of the global registry.
    /// </summary>
    private readonly ConcurrentQueue<string> _insertionOrder = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskRegistryRuntime"/> class.
    /// </summary>
    /// <param name="taskManager">The manager providing global runtime settings and limits.</param>
    /// <param name="taskRegistry">The underlying storage for task snapshots.</param>
    /// <param name="loggerManager">Optional logger manager to provide contextual logging.</param>
    public TaskRegistryRuntime(ITaskManager taskManager, ITaskRegistry taskRegistry, ICustomLoggerManager? loggerManager = null)
    {
        _taskRegistry = taskRegistry;
        _taskManager = taskManager;

        _logger = loggerManager?.GetLogger("Managed Task Registry");
    }

    /// <summary>
    /// Updates or creates a snapshot in the registry based on the current state of a task and its iterations.
    /// </summary>
    /// <param name="managedTaskRuntime">The active task runtime to capture data from.</param>
    /// <param name="iterationRuntime">The specific iteration context to record, if applicable.</param>
    /// <param name="taskException">An exception associated with the overall task failure, if any.</param>
    /// <param name="iterationException">An exception associated with a specific iteration failure, if any.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating whether the snapshot and trimming logic executed successfully.</returns>
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
