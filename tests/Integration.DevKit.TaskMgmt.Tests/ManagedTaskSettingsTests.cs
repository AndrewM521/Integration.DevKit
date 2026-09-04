using Integration.DevKit.TaskMgmt.Models;
using Integration.DevKit.TaskMgmt.Settings;

namespace Integration.DevKit.TaskMgmt.Tests;

public class ManagedTaskSettingsTests
{
    [Fact]
    public void Clone_ProducesIndependentCopy()
    {
        var original = new ManagedTaskSettings { MaxIterations = 5, RetryOnException = true };

        var clone = original.Clone();
        clone.MaxIterations = 10;
        clone.RetryOnException = false;

        Assert.Equal(5, original.MaxIterations);
        Assert.True(original.RetryOnException);
        Assert.Equal(10, clone.MaxIterations);
        Assert.False(clone.RetryOnException);
    }

    [Fact]
    public void Clone_PreservesTimeout()
    {
        var original = new ManagedTaskSettings { Timeout = TimeSpan.FromSeconds(30) };

        var clone = original.Clone();

        Assert.Equal(TimeSpan.FromSeconds(30), clone.Timeout);
    }

    [Theory]
    [InlineData(0, -1)]
    [InlineData(-5, -1)]
    [InlineData(3, 3)]
    public void MaxIterations_NonPositiveValues_NormalizedToNegativeOne(int input, int expected)
    {
        var settings = new ManagedTaskSettings { MaxIterations = input };

        Assert.Equal(expected, settings.MaxIterations);
    }

    [Theory]
    [InlineData(0, -1)]
    [InlineData(-5, -1)]
    [InlineData(3, 3)]
    public void MaxRetryCount_NonPositiveValues_NormalizedToNegativeOne(int input, int expected)
    {
        var settings = new ManagedTaskSettings { MaxRetryCount = input };

        Assert.Equal(expected, settings.MaxRetryCount);
    }
}

public class TaskManagerSettingsTests
{
    [Fact]
    public void Clone_ProducesIndependentCopy()
    {
        var original = new TaskManagerSettings { MaxConcurrentTasks = 5, MaxTaskRegistryCount = 100 };

        var clone = original.Clone();
        clone.MaxConcurrentTasks = 50;

        Assert.Equal(5, original.MaxConcurrentTasks);
        Assert.Equal(50, clone.MaxConcurrentTasks);
        Assert.Equal(100, clone.MaxTaskRegistryCount);
    }
}
