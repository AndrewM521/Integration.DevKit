namespace AndrewM5.DevKit.Threading.Scheduling.Services.ScheduleStrategies;

public class DailyScheduleStrategy : TaskScheduleStrategy
{
    public DailyScheduleStrategy(DateOnly? startDate = null, TimeSpan? startTime = null, bool execImmediately = false) : base(startDate, startTime, execImmediately) { }

    public override DateTime GetNextTargetTime()
    {
        if (LastTargetTime == null)
        {
            if (ExecImmediately)
            {
                return StartDateTimeRef;
            }
            else
            {
                return StartDateTimeRef.AddDays(1);
            }
        }

        return LastTargetTime.Value.AddDays(1);
    }
}
