using AndrewM5.DevKit.Logging.Abstractions.Settings;

namespace AndrewM5.DevKit.Logging.Contracts.Interfaces;

public interface ICustomLoggerManager
{
    public LoggerManagerSettings RuntimeSettings { get; }
    public ICustomLogger GetLogger(string categoryName);
    public void OutputRuntimeSettings();
}
