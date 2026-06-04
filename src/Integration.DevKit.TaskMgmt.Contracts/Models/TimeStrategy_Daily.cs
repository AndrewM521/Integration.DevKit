/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

namespace Integration.DevKit.TaskMgmt.Contracts.Models;

/// <summary>
/// Represents a scheduling strategy that calculates the next execution time on a daily basis.
/// </summary>
public sealed class TimeStrategy_Daily : Time_IterationStrategy
{
    /// <summary>
    /// Initializes a new instance of the <see cref="TimeStrategy_Daily"/> class using the provided settings.
    /// </summary>
    /// <param name="settings">The time-specific configuration, including the start time used for the first iteration.</param>
    public TimeStrategy_Daily(TimeStrategySettings settings) : base(settings) { }

    /// <summary>
    /// Calculates the next execution time by adding exactly one day to the <see cref="Time_IterationStrategy.LastTargetDTM"/>.
    /// </summary>
    /// <param name="currentIteration">The current iteration count of the task.</param>
    /// <returns>
    /// A <see cref="DateTime"/> representing the same time of day as the last target, incremented by one day.
    /// </returns>
    /// <remarks>
    /// By adding the day to the <c>Target</c> time rather than the <c>Current</c> time, this strategy 
    /// maintains a consistent schedule even if the task work takes several minutes or hours to complete.
    /// </remarks>
    protected override DateTime ComputeNextTargetDTM(int currentIteration)
    {
        return LastTargetDTM.AddDays(1);
    }
}
