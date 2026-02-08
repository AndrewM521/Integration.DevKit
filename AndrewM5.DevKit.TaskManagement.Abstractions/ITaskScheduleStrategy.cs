
namespace AndrewM5.DevKit.TaskManagement.Abstractions;

public interface ITaskScheduleStrategy
{
    public DateOnly StartDate { get; }

    public TimeSpan StartTime { get; }

    public bool ExecImmediately { get; }

    public DateTime StartDateTimeRef { get => StartDate.ToDateTime(new TimeOnly(0)) + StartTime; }

    public DateTime? LastTargetTime { get; set; }

    public abstract DateTime GetNextTargetTime();
}
