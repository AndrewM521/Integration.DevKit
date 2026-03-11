using AndrewM5.DevKit.Core.Results;

namespace AndrewM5.DevKit.TaskManagement.Abstractions.Interfaces;

public interface IManagedTaskRegistry
{
    public int Count { get; }

    public NullOperationResult Upsert(IManagedTaskSnapshot snapshot);

    public OperationResult<bool> TryGet(string taskKey, out IManagedTaskSnapshot snapshot);

    public NullOperationResult Remove(string taskKey);

    public IEnumerable<IManagedTaskSnapshot> GetAll();
}
