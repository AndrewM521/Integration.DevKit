namespace AndrewM5.DevKit.TaskManagement.ScheduleStrategies;

/// <summary>
/// Represents a flexible scheduling strategy that calculates the next execution time based on a fixed <see cref="TimeSpan"/> interval.
/// </summary>
public sealed class NextRunStrategy_Interval : TimeBasedContinueIteration
{
    private readonly TimeSpan _interval;

    /// <summary>
    /// Initializes a new instance of the <see cref="NextRunStrategy_Interval"/> class.
    /// </summary>
    /// <param name="interval">The amount of time to add between each execution.</param>
    /// <param name="customStartDate">The specific date to start the schedule. If null, defaults to the current date.</param>
    /// <param name="customStartTime">The specific time of day the schedule should begin. If null, defaults to the current time.</param>
    public NextRunStrategy_Interval(TimeSpan interval, DateOnly? customStartDate = null, TimeSpan? customStartTime = null) : base(customStartDate, customStartTime)
    {
        _interval = interval;
    }

    /// <summary>
    /// Calculates the next execution time by adding the defined interval to the <see cref="TimeBasedContinueIteration.LastTargetDTM"/>.
    /// </summary>
    /// <param name="iteration">The current iteration count of the task.</param>
    /// <returns>A <see cref="DateTime"/> representing the last target time plus the specified interval.</returns>
    protected override DateTime ComputeNextTargetDTM(int iteration)
    {
        return LastTargetDTM.Add(_interval);
    }
}
