using AndrewM5.DevKit.TaskManagement.Abstractions;

namespace AndrewM5.DevKit.TaskManagement.ScheduleStrategies;

/// <summary>
/// Represents a scheduling strategy that calculates the next execution time on a daily basis.
/// </summary>
public class NextRunStrategy_Daily : NextRunStrategy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="NextRunStrategy_Daily"/> class.
    /// </summary>
    /// <param name="startDate">The specific date to start the daily schedule. If null, defaults to the current date.</param>
    /// <param name="startTime">The specific time of day the task should run. If null, defaults to the current time.</param>
    public NextRunStrategy_Daily(DateOnly? startDate = null, TimeSpan? startTime = null) : base(startDate, startTime) { }

    /// <summary>
    /// Calculates the next execution time by adding exactly one day to the <see cref="NextRunStrategy.LastTargetDTM"/>.
    /// </summary>
    /// <param name="currentIteration">The current iteration count of the task.</param>
    /// <returns>A <see cref="DateTime"/> representing the same time on the following day.</returns>
    protected override DateTime ComputeNextTargetDTM(int currentIteration)
    {
        return LastTargetDTM.AddDays(1);
    }
}
