using AndrewM5.DevKit.TaskManagement.Contracts.Interfaces;
using AndrewM5.DevKit.TaskManagement.Contracts.Models;

namespace TestApp;

internal class SimpleLongTask : ManagedTask
{
    public SimpleLongTask() : base("SimpleLongTask", Guid.NewGuid()) {}

    public override async Task DoTaskWork(IManagedTaskIterationHandle iterationHandle)
    {
        Console.WriteLine($"Start Time: {iterationHandle.TaskHandle.StartDTM}");
        Console.WriteLine($"Start Iteration Time: {iterationHandle.StartDTM}");

        for (int i = 0; i < 20; i++)
        {
            Console.WriteLine(i);

            await Task.Delay(500, iterationHandle.Token);
        }

        Console.WriteLine($"End Time: {DateTime.Now}. Elapsed Iteration Time: {iterationHandle.Runtime}");
        Console.WriteLine($"End Time: {DateTime.Now}. Elapsed Time: {iterationHandle.TaskHandle.Runtime}");
    }
}
