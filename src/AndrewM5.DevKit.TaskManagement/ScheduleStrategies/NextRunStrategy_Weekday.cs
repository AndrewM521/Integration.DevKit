using AndrewM5.DevKit.TaskManagement.Abstractions;

namespace AndrewM5.DevKit.TaskManagement.ScheduleStrategies;

/// <summary>
/// Represents a scheduling strategy that calculates the next execution time on the following business day (Monday through Friday).
/// </summary>
public class NextRunStrategy_Weekday : NextRunStrategy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NextRunStrategy_Weekday"/> class.
    /// </summary>
    /// <param name="startDate">The specific date to start the schedule. If null, defaults to the current date.</param>
    /// <param name="startTime">The specific time of day the task should run. If null, defaults to the current time.</param>
    public NextRunStrategy_Weekday(DateOnly? startDate = null, TimeSpan? startTime = null) : base(startDate, startTime) { }

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
