
namespace AndrewM5.DevKit.Logging.Abstractions;

public interface ILogRegistry
{
    public int Count { get; }

    public void EnqueueToLogFileBuffer(string message);
    
    public string[] DequeueFromLogFileBuffer();
    
    public int GetLogFileQueueCount();
}
