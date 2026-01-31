using AndrewM5.DevKit.Threading.Abstractions;
using AndrewM5.DevKit.Threading.Services;
using System.Collections.Concurrent;

namespace AndrewM5.DevKit.Threading.Utilities;

internal class TaskRegistry : ITaskRegistry
{
    private readonly ConcurrentDictionary<string, ManagedTaskSnapshot> _snapshots = new ConcurrentDictionary<string, ManagedTaskSnapshot>();
    private readonly ConcurrentQueue<string> _insertionOrder = new();

    private readonly int _maxEntries;

    public TaskRegistry(int maxEntries = 2000)
    {
        if (maxEntries <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxEntries));
        }

        _maxEntries = maxEntries;
    }

    public int Count => _snapshots.Count;

    public void Upsert(ManagedTaskSnapshot snapshot)
    {
        _snapshots[snapshot.TaskKey] = snapshot;

        _insertionOrder.Enqueue(snapshot.TaskKey);

        TrimIfNeeded();
    }

    public bool TryGet(string taskKey, out ManagedTaskSnapshot snapshot)
    {
        return _snapshots.TryGetValue(taskKey, out snapshot);
    }

    public void Remove(string taskKey)
    {
        _snapshots.TryRemove(taskKey, out _);
    }

    public IEnumerable<ManagedTaskSnapshot> GetAll()
    {
        return _snapshots.Values;
    }

    private void TrimIfNeeded()
    {
        while (_snapshots.Count > _maxEntries && _insertionOrder.TryDequeue(out var oldestKey))
        {
            // only remove if it's still the same entry (key could have been reinserted)
            _snapshots.TryRemove(oldestKey, out _);
        }
    }
}
