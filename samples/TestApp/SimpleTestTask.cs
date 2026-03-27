using AndrewM5.DevKit.TaskManagement;

namespace TestApp;

internal class SimpleTestTask : ManagedTask
{
    public SimpleTestTask() : base("SimpleTestTask", Guid.NewGuid()) {}

    public override async Task DoTaskWork(CancellationToken cancellationToken)
    {
        Console.WriteLine($"Start Local Time: {DateTime.Now}");
        Console.WriteLine($"Start Utc Time: {DateTime.UtcNow}");

        for (int i = 0; i < 20; i++)
        {
            Console.WriteLine(i);

            await Task.Delay(500, cancellationToken);
        }

        throw new NotImplementedException();
    }
}
