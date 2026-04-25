namespace AndrewM5.DevKit.TaskManagement.Contracts.Models;

/// <summary>
/// Represents a scheduling strategy that calculates the next execution time on an hourly basis.
/// </summary>
public sealed class TimeStrategy_Hourly : Time_IterationStrategy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimeStrategy_Hourly"/> class using the provided settings.
    /// </summary>
    /// <param name="settings">The time-specific configuration used for start-time resolution and catch-up policies.</param>
    public TimeStrategy_Hourly(TimeStrategySettings settings) : base(settings) { }

    /// <summary>
    /// Calculates the next execution time by adding exactly one hour to the <see cref="Time_IterationStrategy.LastTargetDTM"/>.
    /// </summary>
    /// <param name="currentIteration">The current iteration count of the task.</param>
    /// <returns>
    /// A <see cref="DateTime"/> representing the time one hour after the previous scheduled target.
    /// </returns>
    /// <remarks>
    /// By adding an hour to the <c>Target</c> time rather than the <c>Current</c> time, this strategy 
    /// maintains a consistent schedule even if the task work takes several minutes or hours to complete.
    /// </remarks>
    protected override DateTime ComputeNextTargetDTM(int currentIteration)
    {
        return LastTargetDTM.AddHours(1);
    }
}
