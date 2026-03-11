using AndrewM5.DevKit.TaskManagement.Abstractions;

namespace AndrewM5.DevKit.TaskManagement.ScheduleStrategies;

public class DailyScheduleStrategy : NextTargetDTMStrategy
{
    public DailyScheduleStrategy(DateOnly? startDate = null, TimeSpan? startTime = null) : base(startDate, startTime) { }

    protected override DateTime ComputeNextTargetDTM(int currentIteration)
    {
        return LastTargetDTM.AddDays(1);
    }
}
