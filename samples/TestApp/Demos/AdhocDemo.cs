/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.TaskMgmt;
using Integration.DevKit.TaskMgmt.Contracts;

namespace TestApp.Demos;

/// <summary>
/// Scratch space for ad-hoc, one-off experiments — starts a recurring <see cref="SimpleShortTask"/>
/// on a 2-minute interval strategy starting at 7 AM, fast-forwarded to now.
/// </summary>
public class AdhocDemo : IDemo
{
    public async Task RunAsync()
    {
        var strategy_7AM_Settings = new TimeStrategySettings
        {
            SkipFirstIterationWait = true,
            FastForwardToPresent = true,
            CustomStartTime = new TimeSpan(7, 0, 0)
        };
        var strategy = new TimeStrategy_Interval(new TimeSpan(0, 2, 0), strategy_7AM_Settings);

        var taskSettings = new ManagedTaskSettings
        {
            IterationStrategy = strategy,
            MaxIterations = -1
        };

        var startTask = await Service_TaskMgmt.TaskManager.StartTask(new SimpleShortTask(), TaskExecutionMode.Asyncronous, taskSettings);
        if (!startTask.MethodSuccess)
        {
            throw startTask.Exception;
        }

        await startTask.Result.RunningTask!;
    }
}
