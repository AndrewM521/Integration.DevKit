using AndrewM5.DevKit.TaskManagement.Abstractions;

namespace AndrewM5.DevKit.TaskManagement.ScheduleStrategies;

public class DailyScheduleStrategy : NextRunStrategy
{
    public DailyScheduleStrategy(DateOnly? startDate = null, TimeSpan? startTime = null) : base(startDate, startTime) { }

    protected override DateTime ComputeNextTargetDTM(int currentIteration)
    {
        return LastTargetDTM.AddHours(1);
    }
}
