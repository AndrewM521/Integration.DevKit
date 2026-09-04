using System.Collections.Concurrent;
using Integration.DevKit.Core;

namespace Integration.DevKit.TaskMgmt;

/// <summary>
/// Thread-safe registry for storing and retrieving metadata snapshots
/// of managed tasks.
/// </summary>
/// <remarks>
/// This registry serves as the "source of truth" for the current state of tasks managed by the system.
/// </remarks>
public class TaskRegistry
{
    /// <summary>
    /// Gets the collection of all current task snapshots, keyed by their unique task identifiers.
    /// </summary>
    /// <value>
    /// A <see cref="ConcurrentDictionary{String, ManagedTaskSnapshot}"/> where the key is the
    /// unique Task ID and the value is the last reported state of that task.
    /// </value>
    public ConcurrentDictionary<string, ManagedTaskSnapshot> Snapshots { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskRegistry"/> class with an empty snapshot collection.
    /// </summary>
    public TaskRegistry() {
        Snapshots = new ConcurrentDictionary<string, ManagedTaskSnapshot>();
    }

    /// <summary>
    /// Attempts to retrieve a snapshot associated with the specified task key.
    /// </summary>
    /// <param name="taskKey">The unique identifier of the task.</param>
    /// <returns>
    /// A <see cref="NullableOperationResult{ManagedTaskSnapshot}"/> containing the snapshot if found,
    /// or a successful result with a null value if the key does not exist.
    /// </returns>
    /// <remarks>
    /// If the key is not found, the result is still considered successful but contains a null value.
    /// </remarks>
    public NullableOperationResult<ManagedTaskSnapshot?> TryGet(string taskKey)
    {
        var result = new NullableOperationResult<ManagedTaskSnapshot?>();

        try
        {
            Snapshots.TryGetValue(taskKey, out ManagedTaskSnapshot? snapshot);

            return result.SetMethodSuccess(snapshot);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Adds a new snapshot to the registry or updates an existing one if the key already exists.
    /// </summary>
    /// <param name="snapshot">The task snapshot containing the current state and metadata to store.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating whether the upsert operation was successful.</returns>
    public NullOperationResult Upsert(ManagedTaskSnapshot snapshot)
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

    /// <summary>
    /// Removes the snapshot associated with the specified task key from the registry.
    /// </summary>
    /// <param name="taskKey">The unique identifier of the task to remove.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating if the removal was successful.</returns>
    /// <remarks>
    /// If the key does not exist, the operation is still returned as successful
    /// </remarks>
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
