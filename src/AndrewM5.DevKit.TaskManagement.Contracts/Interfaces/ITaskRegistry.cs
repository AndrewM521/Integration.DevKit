using AndrewM5.DevKit.Core.Results;
using System.Collections.Concurrent;

namespace AndrewM5.DevKit.TaskManagement.Contracts.Interfaces;

/// <summary>
/// Defines a thread-safe registry for storing and retrieving metadata snapshots 
/// of managed tasks.
/// </summary>
public interface ITaskRegistry
{
    /// <summary>
    /// Gets the collection of all current task snapshots, keyed by their unique task identifiers.
    /// </summary>
    public ConcurrentDictionary<string, IManagedTaskSnapshot> Snapshots { get; }

    /// <summary>
    /// Adds a new snapshot to the registry or updates an existing one if the key already exists.
    /// </summary>
    /// <param name="snapshot">The task snapshot containing the current state and metadata to store.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating whether the upsert operation was successful.</returns>
    public NullOperationResult Upsert(IManagedTaskSnapshot snapshot);

    /// <summary>
    /// Attempts to retrieve a snapshot associated with the specified task key.
    /// </summary>
    /// <param name="taskKey">The unique identifier of the task.</param>
    /// <returns>
    /// A <see cref="NullableOperationResult{IManagedTaskSnapshot}"/> containing the snapshot if found, 
    /// or a successful result with a null value if the key does not exist.
    /// </returns>
    public NullableOperationResult<IManagedTaskSnapshot?> TryGet(string taskKey);

    /// <summary>
    /// Removes the snapshot associated with the specified task key from the registry.
    /// </summary>
    /// <param name="taskKey">The unique identifier of the task to remove.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating if the removal was successful.</returns>
    public NullOperationResult Remove(string taskKey);
}
