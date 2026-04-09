using AndrewM5.DevKit.TaskManagement;
using AndrewM5.DevKit.TaskManagement.Contracts.Models;

namespace TestApp;

internal class SimpleBrokenTask : ManagedTask
{
    public SimpleBrokenTask() : base("SimpleBrokenTask", Guid.NewGuid()) {}

    public override async Task DoTaskWork(CancellationToken cancellationToken)
    {
        Console.WriteLine($"Start Time: {Handle?.StartTime}");
        Console.WriteLine($"Start Iteration Time: {Handle?.IterationStartTime}");

        for (int i = 0; i < 5; i++)
        {
            Console.WriteLine(i);

            await Task.Delay(500, cancellationToken);
        }

        Console.WriteLine($"End Time: {DateTime.Now}. Elapsed Iteration Time: {Handle?.GetTaskIterationRuntime().Result}");
        Console.WriteLine($"End Time: {DateTime.Now}. Elapsed Time: {Handle?.GetTaskRuntime().Result}");

        throw new NotImplementedException();
    }
}
