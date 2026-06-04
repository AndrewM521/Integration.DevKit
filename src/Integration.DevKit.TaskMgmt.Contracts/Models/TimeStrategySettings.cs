/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.DevKit.TaskMgmt.Contracts.Models;

/// <summary>
/// Contains configuration settings that define how time-based iteration strategies calculate 
/// start times and handle missed execution windows.
/// </summary>
public class TimeStrategySettings
{
    /// <summary>
    /// Gets or sets a value indicating whether the task should execute immediately upon starting 
    /// without waiting for the first scheduled time slot.
    /// </summary>
    /// <value>Default is <see langword="true"/>.</value>
    public bool SkipFirstIterationWait { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the strategy should skip iterations that were 
    /// missed while the task or service was inactive.
    /// </summary>
    /// <remarks>
    /// When <see langword="true"/>, the strategy will "burn" through past schedules until it finds 
    /// the next execution target in the future. This prevents the task manager from attempting 
    /// to "catch up" by running multiple iterations back-to-back for time already passed.
    /// </remarks>
    /// <value>Default is <see langword="true"/>.</value>
    public bool FastForwardToPresent { get; set; } = true;

    /// <summary>
    /// Gets or sets a specific date the strategy should begin its calculations from.
    /// </summary>
    /// <value>
    /// A <see cref="DateOnly"/> instance. If <see langword="null"/>, the strategy 
    /// defaults to the current system date.
    /// </value>
    public DateOnly? CustomStartDate { get; set; }

    /// <summary>
    /// Gets or sets a specific time of day the strategy should begin its calculations from.
    /// </summary>
    /// <value>
    /// A <see cref="TimeSpan"/> representing the time of day. If <see langword="null"/>, 
    /// the strategy defaults to the current system time.
    /// </value>
    public TimeSpan? CustomStartTime { get; set; }
}
