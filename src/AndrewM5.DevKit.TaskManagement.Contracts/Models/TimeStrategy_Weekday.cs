namespace AndrewM5.DevKit.TaskManagement.Contracts.Models;

/// <summary>
/// Represents a scheduling strategy that calculates the next execution time on the following business day (Monday through Friday).
/// </summary>
public sealed class TimeStrategy_Weekday : Time_IterationStrategy
{
    public TimeStrategy_Weekday(TimeStrategySettings settings) : base(settings) { }

    /// <summary>
    /// Calculates the next execution time by incrementing the day until a weekday (Monday-Friday) is found.
    /// </summary>
    /// <param name="currentIteration">The current iteration count of the task.</param>
    /// <returns>
    /// A <see cref="DateTime"/> representing the next available weekday at the same time of day as the last target.
    /// </returns>
    protected override DateTime ComputeNextTargetDTM(int currentIteration)
    {
        DateTime targetDay = LastTargetDTM.AddDays(1);

        while (targetDay.DayOfWeek == DayOfWeek.Saturday || targetDay.DayOfWeek == DayOfWeek.Sunday)
        {
            targetDay = targetDay.AddDays(1);
        }

        return targetDay;
    }
}
