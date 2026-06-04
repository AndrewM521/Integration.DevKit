/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.Core.Results;
using System.Collections.Concurrent;

namespace Integration.DevKit.TaskMgmt.Contracts.Interfaces;

/// <summary>
/// Defines a thread-safe registry for storing and retrieving metadata snapshots 
/// of managed tasks.
/// </summary>
/// <remarks>
/// This registry serves as the "source of truth" for the current state of tasks managed by the system.
/// </remarks>
public interface ITaskRegistry
{
    /// <summary>
    /// Gets the collection of all current task snapshots, keyed by their unique task identifiers.
    /// </summary>
    /// <value>
    /// A <see cref="ConcurrentDictionary{String, IManagedTaskSnapshot}"/> where the key is the 
    /// unique Task ID and the value is the last reported state of that task.
    /// </value>
    public ConcurrentDictionary<string, IManagedTaskSnapshot> Snapshots { get; }

    /// <summary>
    /// Adds a new snapshot to the registry or updates an existing one if the key already exists.
    /// </summary>
    /// <param name="snapshot">The task snapshot containing the current state and metadata to store.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating whether the upsert operation was successful.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="snapshot"/> is null.</exception>
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
