using AndrewM5.DevKit.TaskManagement.Abstractions;

namespace AndrewM5.DevKit.TaskManagement.ScheduleStrategies;

public class NextRunStrategy_Weekday : NextRunStrategy
{
    public NextRunStrategy_Weekday(DateOnly? startDate = null, TimeSpan? startTime = null) : base(startDate, startTime) { }

    protected override DateTime ComputeNextTargetDTM(int currentIteration)
    {
        DateTime targetDay = LastTargetDTM.AddDays(1);

        while (targetDay.DayOfWeek == DayOfWeek.Saturday || targetDay.DayOfWeek == DayOfWeek.Sunday)
        {
            targetDay = targetDay.AddDays(1);
        }

        return targetDay;
    }
}
