/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

namespace AndrewM5.DevKit.TaskMgmt.Contracts.Models;

/// <summary>
/// Represents a flexible scheduling strategy that calculates the next execution time based on a fixed <see cref="TimeSpan"/> interval.
/// </summary>
public sealed class TimeStrategy_Interval : Time_IterationStrategy
{
    private readonly TimeSpan _interval;

    /// <summary>
    /// Initializes a new instance of the <see cref="TimeStrategy_Interval"/> class with a specific interval and configuration.
    /// </summary>
    /// <param name="interval">The amount of time to wait between the start of each execution cycle.</param>
    /// <param name="settings">The time-specific configuration used for start-time resolution and catch-up policies.</param>
    public TimeStrategy_Interval(TimeSpan interval, TimeStrategySettings settings) : base(settings)
    {
        _interval = interval;
    }

    /// <summary>
    /// Calculates the next execution time by adding the defined interval to the <see cref="Time_IterationStrategy.LastTargetDTM"/>.
    /// </summary>
    /// <param name="iteration">The current iteration count of the task.</param>
    /// <returns>
    /// A <see cref="DateTime"/> representing the previous target time incremented by the configured <see cref="TimeSpan"/>.
    /// </returns>
    /// <remarks>
    /// By adding the interval to the <c>Target</c> time rather than the <c>Current</c> time, this strategy 
    /// maintains a consistent schedule even if the task work takes several minutes or hours to complete.
    /// </remarks>
    protected override DateTime ComputeNextTargetDTM(int iteration)
    {
        return LastTargetDTM.Add(_interval);
    }
}
