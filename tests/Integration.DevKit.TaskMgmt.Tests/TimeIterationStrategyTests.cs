using Integration.DevKit.TaskMgmt.Contracts;

namespace Integration.DevKit.TaskMgmt.Tests;

public class TimeIterationStrategyTests
{
    [Fact]
    public void TimeStrategy_Daily_ComputesNextTarget_OneDayAfterLast()
    {
        var strategy = new TimeStrategy_Daily(new TimeStrategySettings());
        strategy.LastTargetDTM = new DateTime(2026, 1, 1, 8, 0, 0);

        var next = strategy.GetNextTargetDTM(currentIteration: 1);

        Assert.Equal(new DateTime(2026, 1, 2, 8, 0, 0), next);
    }

    [Fact]
    public void TimeStrategy_Hourly_ComputesNextTarget_OneHourAfterLast()
    {
        var strategy = new TimeStrategy_Hourly(new TimeStrategySettings());
        strategy.LastTargetDTM = new DateTime(2026, 1, 1, 8, 0, 0);

        var next = strategy.GetNextTargetDTM(currentIteration: 1);

        Assert.Equal(new DateTime(2026, 1, 1, 9, 0, 0), next);
    }

    [Fact]
    public void TimeStrategy_Interval_ComputesNextTarget_IntervalAfterLast()
    {
        var strategy = new TimeStrategy_Interval(TimeSpan.FromMinutes(15), new TimeStrategySettings());
        strategy.LastTargetDTM = new DateTime(2026, 1, 1, 8, 0, 0);

        var next = strategy.GetNextTargetDTM(currentIteration: 1);

        Assert.Equal(new DateTime(2026, 1, 1, 8, 15, 0), next);
    }

    [Fact]
    public void GetNextTargetDTM_WhenLastTargetIsUnset_InitializesFromCustomStartDateAndTime()
    {
        var settings = new TimeStrategySettings
        {
            CustomStartDate = new DateOnly(2026, 6, 1),
            CustomStartTime = TimeSpan.FromHours(9)
        };
        var strategy = new TimeStrategy_Daily(settings);

        var next = strategy.GetNextTargetDTM(currentIteration: 0);

        // First call seeds LastTargetDTM from the custom start, then Daily adds one day on top.
        Assert.Equal(new DateTime(2026, 6, 2, 9, 0, 0), next);
    }
}
