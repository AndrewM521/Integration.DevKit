namespace AndrewM5.DevKit.TaskManagement.Scheduling.Services.ScheduleStrategies;

public sealed class IntervalScheduleStrategy : TaskScheduleStrategy
{
    private readonly TimeSpan _interval;

    public IntervalScheduleStrategy(TimeSpan interval, DateOnly? startDate = null, TimeSpan? startTime = null, bool execImmediately = true) : base(startDate, startTime, execImmediately) {
        _interval = interval;
    }

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
                return StartDateTimeRef.Add(_interval);
            }
        }

        return LastTargetTime.Value.Add(_interval);
    }
}
