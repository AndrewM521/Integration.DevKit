using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.Threading.Scheduling.Services;
using AndrewM5.DevKit.Threading.Services;

namespace AndrewM5.DevKit.Threading.Scheduling.Abstractions;

public interface ITaskSchedulerService
{
    public OperationResult<Task> ScheduleTask(string taskKey, Func<ManagedTask> taskFactory, TaskScheduleStrategy strategy, int maxRunCount = -1);
    public OperationResult<bool> CancelScheduledTask(string taskKey);
    public OperationResult<bool> StartAllSchedules();
    public OperationResult<bool> StopAllSchedules();
}
