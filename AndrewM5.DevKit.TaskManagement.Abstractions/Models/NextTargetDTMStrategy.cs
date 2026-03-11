using Microsoft.VisualBasic;

namespace AndrewM5.DevKit.TaskManagement.Abstractions;

public abstract class NextTargetDTMStrategy
{
    public DateOnly? CustomStartDate { get; }
    public TimeSpan? CustomStartTime { get; }
    public DateTime LastTargetDTM { get; set; } = default;

    protected NextTargetDTMStrategy(DateOnly? startDate = null, TimeSpan? startTime = null)
    {
        CustomStartDate = startDate;
        CustomStartTime = startTime;
    }

    private protected DateTime GetStartDTM()
    {
        var now = DateTime.Now;
        var startDate = DateOnly.FromDateTime(now);
        var startTime = now.TimeOfDay;

        if (CustomStartDate.HasValue)
        {
            startDate = (DateOnly)CustomStartDate;
        }

        if (CustomStartTime.HasValue)
        {
            startTime = (TimeSpan)CustomStartTime;
        }

        var localDTM = startDate.ToDateTime(TimeOnly.FromTimeSpan(startTime));

        return DateTime.SpecifyKind(localDTM, DateTimeKind.Local);
    }

    public DateTime GetNextTargetDTM(int currentIteration)
    {
        if (LastTargetDTM == default)
        {
            return GetStartDTM();
        }

        return ComputeNextTargetDTM(currentIteration);
    }

    protected abstract DateTime ComputeNextTargetDTM(int currentIteration);
}
