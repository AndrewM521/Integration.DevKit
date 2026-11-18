using Integration.DevKit.TaskMgmt.Implementations;
using Integration.DevKit.TaskMgmt.Interfaces;
using Integration.DevKit.TaskMgmt.Models;
using Moq;

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

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void TimeStrategy_Interval_NonPositiveInterval_Throws(int seconds)
    {
        // A zero interval would spin the catch-up loop forever
        Assert.Throws<ArgumentOutOfRangeException>(() => new TimeStrategy_Interval(TimeSpan.FromSeconds(seconds), new TimeStrategySettings()));
    }

    [Fact]
    public void GetNextTargetDTM_WhenStartTimeIsAhead_ReturnsTheStartTimeItself()
    {
        // A start time later today used to be pushed a whole interval out, so the first day was skipped
        var start = DateTime.Now.AddHours(1);
        var strategy = new TimeStrategy_Daily(new TimeStrategySettings
        {
            CustomStartDate = DateOnly.FromDateTime(start),
            CustomStartTime = start.TimeOfDay
        });

        Assert.Equal(start, strategy.GetNextTargetDTM(currentIteration: 0));
    }

    private static IManagedTaskHandle Handle(long iterationCount)
    {
        var handle = new Mock<IManagedTaskHandle>();
        handle.SetupGet(h => h.TaskKey).Returns("strategy-test");
        handle.SetupGet(h => h.CurrentIterationCount).Returns(iterationCount);

        return handle.Object;
    }

    [Fact]
    public async Task WaitForReadyAsync_FastForward_SkipsMissedSlotsAndLandsOnTheLatestOne()
    {
        // A 1-second interval started a year ago: ~31M missed slots, which used to be stepped through one by one
        var strategy = new TimeStrategy_Interval(TimeSpan.FromSeconds(1), new TimeStrategySettings
        {
            CustomStartDate = DateOnly.FromDateTime(DateTime.Now.AddYears(-1)),
            CustomStartTime = TimeSpan.Zero,
            FastForwardToPresent = true,
            SkipFirstIterationWait = true
        });

        var timer = System.Diagnostics.Stopwatch.StartNew();
        await strategy.WaitForReadyAsync(Handle(0), CancellationToken.None);
        timer.Stop();

        Assert.True(timer.Elapsed < TimeSpan.FromSeconds(1), $"Catch-up took {timer.Elapsed}");
        // Skipping the first wait runs the next future slot, at most one interval away (allowing for time passing since)
        var aheadBy = strategy.LastTargetDTM - DateTime.Now;
        Assert.True(aheadBy > TimeSpan.FromSeconds(-1) && aheadBy <= TimeSpan.FromSeconds(1), $"Target is {aheadBy} from now");
    }

    [Fact]
    public async Task WaitForReadyAsync_WithoutFastForward_RunsTheMissedSlotImmediately()
    {
        var start = DateTime.Now.AddHours(-3);
        var strategy = new TimeStrategy_Hourly(new TimeStrategySettings
        {
            CustomStartDate = DateOnly.FromDateTime(start),
            CustomStartTime = start.TimeOfDay
        });

        var waited = strategy.WaitForReadyAsync(Handle(1), CancellationToken.None);

        Assert.True(waited.IsCompleted, "A slot in the past must not wait");
        await waited;
        // The first missed slot (one hour after the start), not one skipped to the present
        Assert.Equal(start.AddHours(1), strategy.LastTargetDTM);
    }
}
