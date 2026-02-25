using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.TaskManagement.Abstractions;
using AndrewM5.DevKit.TaskManagement.Services;
using System.Collections.Concurrent;

namespace AndrewM5.DevKit.TaskManagement.Utilities;

internal class TaskScheduleRegistry : ITaskScheduleRegistry
{
    public int Count => _snapshots.Count;

    private readonly ConcurrentDictionary<string, ITaskScheduleSnapshot> _snapshots = new ConcurrentDictionary<string, ITaskScheduleSnapshot>();

    private readonly ConcurrentQueue<string> _insertionOrder = new();

    private readonly int _maxEntries;

    public TaskScheduleRegistry(int maxEntries = 2000)
    {
        if (maxEntries <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxEntries));
        }

        _maxEntries = maxEntries;
    }

    public NullOperationResult Upsert(string scheduleKey, IManagedTaskSnapshot snapshot)
    {
        var result = new NullOperationResult();

        try
        {
            var entry = _snapshots.GetOrAdd(scheduleKey, key =>
            {
                _insertionOrder.Enqueue(key);

                return new TaskScheduleSnapshot(key);
            });

            entry.Snapshots.Enqueue(snapshot);

            TrimIfNeeded();

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }
    
    public OperationResult<bool> TryGet(string taskKey, out ITaskScheduleSnapshot snapshot)
    {
        var result = new OperationResult<bool>();

        try
        {
            return result.SetMethodSuccess(_snapshots.TryGetValue(taskKey, out snapshot!));
        }
        catch (Exception ex)
        {
            snapshot = null!;
            return result.SetMethodFailure(ex);
        }
    }

    public NullOperationResult Remove(string taskKey)
    {
        var result = new NullOperationResult();

        try
        {
            _snapshots.TryRemove(taskKey, out _);

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    public IEnumerable<ITaskScheduleSnapshot> GetAll()
    {
        return _snapshots.Values;
    }

    private NullOperationResult TrimIfNeeded()
    {
        var result = new NullOperationResult();
        var errors = new List<Exception>();

        while (_snapshots.Count > _maxEntries && _insertionOrder.TryDequeue(out var oldestKey))
        {
            try
            {
                // only remove if it's still the same entry (key could have been reinserted)
                _snapshots.TryRemove(oldestKey, out _);
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
