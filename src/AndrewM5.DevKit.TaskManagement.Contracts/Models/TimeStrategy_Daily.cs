namespace AndrewM5.DevKit.TaskManagement.Contracts.Models;

/// <summary>
/// Represents a scheduling strategy that calculates the next execution time on a daily basis.
/// </summary>
public sealed class TimeStrategy_Daily : Time_IterationStrategy
{
    public TimeStrategy_Daily(TimeStrategySettings settings) : base(settings) { }

    /// <summary>
    /// Calculates the next execution time by adding exactly one day to the <see cref="Time_IterationStrategy.LastTargetDTM"/>.
    /// </summary>
    /// <param name="currentIteration">The current iteration count of the task.</param>
    /// <returns>A <see cref="DateTime"/> representing the same time on the following day.</returns>
    protected override DateTime ComputeNextTargetDTM(int currentIteration)
    {
        return LastTargetDTM.AddDays(1);
    }
}
