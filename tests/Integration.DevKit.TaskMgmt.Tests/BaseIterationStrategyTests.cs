using Integration.DevKit.TaskMgmt.Abstractions;
using Integration.DevKit.TaskMgmt.Interfaces;
using Moq;

namespace Integration.DevKit.TaskMgmt.Tests;

public class BaseIterationStrategyTests
{
    [Fact]
    public async Task WaitForReadyAsync_CompletesImmediately()
    {
        var strategy = new BaseIterationStrategy();
        var handle = new Mock<IManagedTaskHandle>();

        var task = strategy.WaitForReadyAsync(handle.Object, CancellationToken.None);

        Assert.True(task.IsCompleted);
        await task;
    }
}
