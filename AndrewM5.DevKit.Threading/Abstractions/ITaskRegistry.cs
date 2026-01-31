using AndrewM5.DevKit.Threading.Services;

namespace AndrewM5.DevKit.Threading.Abstractions;

public interface ITaskRegistry
{
    int Count { get; }

    void Upsert(ManagedTaskSnapshot snapshot);

    bool TryGet(string taskKey, out ManagedTaskSnapshot snapshot);

    void Remove(string taskKey);

    IEnumerable<ManagedTaskSnapshot> GetAll();
}
