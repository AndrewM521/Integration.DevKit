namespace AndrewM5.DevKit.TaskManagement.Abstractions;

/// <summary>
/// Provides a base implementation for defining when a task should next execute.
/// Handles initial start times and tracking of the last execution target.
/// </summary>
public abstract class NextRunStrategy
{
    /// <summary>
    /// Gets the specific date the strategy should begin from. If null, defaults to the current date.
    /// </summary>
    public DateOnly? CustomStartDate { get; }

    /// <summary>
    /// Gets the specific time of day the strategy should begin from. If null, defaults to the current time.
    /// </summary>
    public TimeSpan? CustomStartTime { get; }

    /// <summary>
    /// Gets or sets the timestamp of the last calculated execution target.
    /// Used as a reference point for calculating the subsequent run.
    /// </summary>
    public DateTime LastTargetDTM { get; set; } = default;

    /// <summary>
    /// Initializes a new instance of the <see cref="NextRunStrategy"/> class.
    /// </summary>
    /// <param name="startDate">An optional starting date.</param>
    /// <param name="startTime">An optional starting time of day.</param>
    protected NextRunStrategy(DateOnly? startDate = null, TimeSpan? startTime = null)
    {
        CustomStartDate = startDate;
        CustomStartTime = startTime;
    }

    /// <summary>
    /// Resolves the effective starting <see cref="DateTime"/> by combining custom or default date and time values.
    /// </summary>
    /// <returns>A local <see cref="DateTime"/> representing the absolute start point.</returns>
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

    /// <summary>
    /// Determines the next scheduled execution time. 
    /// If no previous run has occurred, it returns the start time; otherwise, it triggers the inherited computation logic.
    /// </summary>
    /// <param name="currentIteration">The current iteration count of the task.</param>
    /// <returns>The <see cref="DateTime"/> for the next execution.</returns>
    public DateTime GetNextTargetDTM(int currentIteration)
    {
        if (LastTargetDTM == default)
        {
            return GetStartDTM();
        }

        return ComputeNextTargetDTM(currentIteration);
    }

    /// <summary>
    /// When implemented in a derived class, calculates the next execution time based on the strategy's specific recurrence rules.
    /// </summary>
    /// <param name="currentIteration">The current iteration count of the task.</param>
    /// <returns>The calculated <see cref="DateTime"/> for the next run.</returns>
    protected abstract DateTime ComputeNextTargetDTM(int currentIteration);
}
