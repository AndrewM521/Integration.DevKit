using AndrewM5.DevKit.TaskManagement;
using AndrewM5.DevKit.TaskManagement.Contracts.Models;

namespace TestApp;

internal class SimpleShortTask : ManagedTask
{
    public SimpleShortTask() : base("SimpleShortTask", Guid.NewGuid()) {}

    public override async Task DoTaskWork(CancellationToken cancellationToken)
    {
        Console.WriteLine($"Start Local Time: {DateTime.Now}");
        Console.WriteLine($"Start Utc Time: {DateTime.UtcNow}");

        for (int i = 0; i < 5; i++)
        {
            Console.WriteLine(i);

            await Task.Delay(500, cancellationToken);
        }
    }
}
