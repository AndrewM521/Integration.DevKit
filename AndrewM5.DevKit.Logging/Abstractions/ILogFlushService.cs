using AndrewM5.DevKit.Logging.Settings;

namespace AndrewM5.DevKit.Logging.Abstractions;

public interface ILogFlushService
{
    public LogFlushServiceSettings RuntimeSettings { get; }
    public void DisplayRuntimeSettings();
}
