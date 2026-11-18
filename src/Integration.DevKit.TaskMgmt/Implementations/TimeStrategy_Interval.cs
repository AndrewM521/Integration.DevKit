/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.TaskMgmt.Abstractions;
using Integration.DevKit.TaskMgmt.Models;

namespace Integration.DevKit.TaskMgmt.Implementations;

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
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="interval"/> is zero or negative.</exception>
    public TimeStrategy_Interval(TimeSpan interval, TimeStrategySettings settings) : base(settings)
    {
        // A non-positive interval never moves the target forward, so the catch-up loop would spin forever
        if (interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(interval), "Interval must be greater than zero.");
        }

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
    protected override DateTime ComputeNextTargetDTM(long iteration)
    {
        // Add in UTC so daylight-saving changes do not stretch or shrink the interval
        return LastTargetDTM.ToUniversalTime().Add(_interval).ToLocalTime();
    }

    /// <inheritdoc />
    protected override DateTime SkipToPresent(DateTime target, long currentIteration, CancellationToken cancellationToken)
    {
        TimeSpan behind = DateTime.UtcNow - target.ToUniversalTime();
        if (behind < TimeSpan.Zero)
        {
            return target;
        }

        // Jump straight to the last missed slot: stepping one by one takes millions of loops for small intervals and old start dates
        long missedSlots = behind.Ticks / _interval.Ticks;
        LastTargetDTM = target.ToUniversalTime().AddTicks(missedSlots * _interval.Ticks).ToLocalTime();

        return ComputeNextTargetDTM(currentIteration);
    }
}
