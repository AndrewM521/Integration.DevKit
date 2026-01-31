namespace AndrewM5.DevKit.Threading.Scheduling.Services;

public abstract class TaskScheduleStrategy
{
    public DateOnly StartDate { get; private set; }

    public TimeSpan StartTime { get; private set; }

    public bool ExecImmediately { get; private set; }

    public DateTime StartDateTimeRef { get => StartDate.ToDateTime(new TimeOnly(0)) + StartTime; }

    internal DateTime? LastTargetTime { get; set; }

    public TaskScheduleStrategy(DateOnly? startDate = null, TimeSpan? startTime = null, bool execImmediately = true)
    {
        if (startDate == null)
        {
            StartDate = DateOnly.FromDateTime(DateTime.Now);
        }
        else
        {
            StartDate = (DateOnly)startDate;
        }

        if (startTime == null)
        {
            StartTime = DateTime.Now.TimeOfDay;
        }
        else
        {
            StartTime = (TimeSpan)startTime;
        }

        ExecImmediately = execImmediately;
    }

    public abstract DateTime GetNextTargetTime();
}
