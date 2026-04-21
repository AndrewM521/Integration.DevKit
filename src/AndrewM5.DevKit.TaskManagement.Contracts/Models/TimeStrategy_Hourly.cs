namespace AndrewM5.DevKit.TaskManagement.Contracts.Models;

/// <summary>
/// Represents a scheduling strategy that calculates the next execution time on an hourly basis.
/// </summary>
public sealed class TimeStrategy_Hourly : Time_IterationStrategy
{
    public TimeStrategy_Hourly(TimeStrategySettings settings) : base(settings) { }

    /// <summary>
    /// Calculates the next execution time by adding exactly one hour to the <see cref="Time_IterationStrategy.LastTargetDTM"/>.
    /// </summary>
    /// <param name="currentIteration">The current iteration count of the task.</param>
    /// <returns>A <see cref="DateTime"/> representing the time one hour after the previous target.</returns>
    protected override DateTime ComputeNextTargetDTM(int currentIteration)
    {
        return LastTargetDTM.AddHours(1);
    }
}
