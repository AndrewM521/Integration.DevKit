using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.TaskManagement.Abstractions;

namespace AndrewM5.DevKit.TaskManagement.Scheduling.Abstractions;

public interface ITaskScheduleService
{
    public OperationResult<Task> ScheduleTask(string taskKey, Func<IManagedTask> taskFactory, ITaskScheduleStrategy strategy, int maxRunCount = -1);
    public OperationResult<bool> CancelScheduledTask(string taskKey);
    public OperationResult<bool> StartAllSchedules();
    public OperationResult<bool> StopAllSchedules();
}
