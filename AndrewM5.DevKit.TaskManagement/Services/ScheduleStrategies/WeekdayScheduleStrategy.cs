namespace AndrewM5.DevKit.TaskManagement.Services.ScheduleStrategies;

public class WeekdayScheduleStrategy : TaskScheduleStrategy
{
    public WeekdayScheduleStrategy(DateOnly? startDate = null, TimeSpan? startTime = null, bool execImmediately = false) : base(startDate, startTime, execImmediately) {}

    public override DateTime GetNextTargetTime()
    {
        DateTime targetDay;

        if (LastTargetTime == null)
        {
            if (ExecImmediately)
            {
                targetDay = StartDateTimeRef;
            }
            else
            {
                targetDay = StartDateTimeRef.AddDays(1);
            }
        }
        else
        {
            targetDay = LastTargetTime.Value.AddDays(1);
        }

        while (targetDay.DayOfWeek == DayOfWeek.Saturday || targetDay.DayOfWeek == DayOfWeek.Sunday)
        {
            targetDay = targetDay.AddDays(1);
        }

        return targetDay;
    }
}
