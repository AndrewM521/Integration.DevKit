using AndrewM5.DevKit.Core.Results;

namespace AndrewM5.DevKit.TaskManagement.Abstractions;

public interface ITaskScheduleRegistry
{
    public int Count { get; }

    public NullOperationResult Upsert(string scheduleKey, IManagedTaskSnapshot snapshot);

    public OperationResult<bool> TryGet(string taskKey, out ITaskScheduleSnapshot snapshot);

    public NullOperationResult Remove(string taskKey);

    public IEnumerable<ITaskScheduleSnapshot> GetAll();
}
