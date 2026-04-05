using AndrewM5.DevKit.TaskManagement;

namespace TestApp;

internal class SimpleBrokenTask : ManagedTask
{
    public SimpleBrokenTask() : base("SimpleBrokenTask", Guid.NewGuid()) {}

    public override async Task DoTaskWork(CancellationToken cancellationToken)
    {
        Console.WriteLine($"Start Local Time: {DateTime.Now}");
        Console.WriteLine($"Start Utc Time: {DateTime.UtcNow}");

        for (int i = 0; i < 5; i++)
        {
            Console.WriteLine(i);

            await Task.Delay(500, cancellationToken);
        }

        throw new NotImplementedException();
    }
}
