/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.TaskMgmt.Contracts.Interfaces;
using System.Collections.Concurrent;

namespace AndrewM5.DevKit.TaskMgmt;

/// <summary>
/// Concrete Implementation of <see cref="ITaskRegistry"/>
/// </summary>
internal class TaskRegistry : ITaskRegistry
{
    /// <inheritdoc/>
    public ConcurrentDictionary<string, IManagedTaskSnapshot> Snapshots { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskRegistry"/> class with an empty snapshot collection.
    /// </summary>
    public TaskRegistry() {
        Snapshots = new ConcurrentDictionary<string, IManagedTaskSnapshot>();
    }

    /// <inheritdoc/>
    /// <remarks>
    /// If the key is not found, the result is still considered successful but contains a null value.
    /// </remarks>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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
