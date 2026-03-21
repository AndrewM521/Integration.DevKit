using AndrewM5.DevKit.TaskManagement.Abstractions;

namespace AndrewM5.DevKit.TaskManagement.ScheduleStrategies;

public sealed class NextRunStrategy_Interval : NextRunStrategy
{
    private readonly TimeSpan _interval;

    public NextRunStrategy_Interval(TimeSpan interval, DateOnly? customStartDate = null, TimeSpan? customStartTime = null) : base(customStartDate, customStartTime)
    {
        _interval = interval;
    }

    protected override DateTime ComputeNextTargetDTM(int iteration)
    {
        return LastTargetDTM.Add(_interval);
    }
}
