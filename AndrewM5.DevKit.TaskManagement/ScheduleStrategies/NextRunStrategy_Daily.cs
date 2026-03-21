using AndrewM5.DevKit.TaskManagement.Abstractions;

namespace AndrewM5.DevKit.TaskManagement.ScheduleStrategies;

public class NextRunStrategy_Daily : NextRunStrategy
{
    public NextRunStrategy_Daily(DateOnly? startDate = null, TimeSpan? startTime = null) : base(startDate, startTime) { }

    protected override DateTime ComputeNextTargetDTM(int currentIteration)
    {
        return LastTargetDTM.AddDays(1);
    }
}
