using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.TaskManagement.Abstractions;

namespace AndrewM5.DevKit.TaskManagement.Scheduling.Abstractions;

public interface ITaskScheduleService
{
    public OperationResult<Task> ScheduleTask(string taskKey, Func<IManagedTask> taskFactory, ITaskScheduleStrategy strategy, int maxRunCount = -1);
    public NullOperationResult CancelScheduledTask(string taskKey);
    public NullOperationResult StartAllSchedules();
    public NullOperationResult StopAllSchedules();
}
