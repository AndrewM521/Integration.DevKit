namespace AndrewM5.DevKit.TaskManagement.ScheduleStrategies;

/// <summary>
/// Represents a scheduling strategy that calculates the next execution time on an hourly basis.
/// </summary>
public sealed class NextRunStrategy_Hourly : TimeBasedContinueIteration
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NextRunStrategy_Hourly"/> class.
    /// </summary>
    /// <param name="startDate">The specific date to start the hourly schedule. If null, defaults to the current date.</param>
    /// <param name="startTime">The specific time of day the hourly sequence should begin. If null, defaults to the current time.</param>
    public NextRunStrategy_Hourly(DateOnly? startDate = null, TimeSpan? startTime = null) : base(startDate, startTime) { }

    /// <summary>
    /// Calculates the next execution time by adding exactly one hour to the <see cref="TimeBasedContinueIteration.LastTargetDTM"/>.
    /// </summary>
    /// <param name="currentIteration">The current iteration count of the task.</param>
    /// <returns>A <see cref="DateTime"/> representing the time one hour after the previous target.</returns>
    protected override DateTime ComputeNextTargetDTM(int currentIteration)
    {
        return LastTargetDTM.AddHours(1);
    }
}
