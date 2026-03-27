using Microsoft.Extensions.Logging;

namespace AndrewM5.DevKit.Logging.Contracts.Interfaces;

public interface ICustomLogger : ILogger
{
    public string CategoryName { get; }   
    public bool IsLoggerEnabled { get; }
    public void EnableLogger();
    public void DisableLogger();
    public void EnableConsoleOutput();
    public void DisableConsoleOutput();
}
