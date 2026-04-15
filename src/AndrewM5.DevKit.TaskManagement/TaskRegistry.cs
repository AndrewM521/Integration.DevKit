using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.TaskManagement.Contracts.Interfaces;
using System.Collections.Concurrent;

namespace AndrewM5.DevKit.TaskManagement;

/// <summary>
/// An internal implementation of <see cref="ITaskRegistry"/> that utilizes a 
/// <see cref="ConcurrentDictionary{TKey, TValue}"/> to manage task metadata in a thread-safe manner.
/// </summary>
internal class TaskRegistry : ITaskRegistry
{
    /// <inheritdoc/>
    public ConcurrentDictionary<string, IManagedTaskSnapshot> Snapshots { get; private set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="TaskRegistry"/> class.
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
    /// <remarks>
    /// Uses the dictionary indexer to ensure that any existing snapshot with the same key is overwritten.
    /// </remarks>
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
    /// If the key does not exist, the operation is still returned as successful, 
    /// as the desired end state (the key being absent) is achieved.
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
