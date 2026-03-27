using AndrewM5.DevKit.Logging.Abstractions.Settings;

namespace AndrewM5.DevKit.Logging.Abstractions;

public interface ILogFlusher
{
    public LogFlushServiceSettings RuntimeSettings { get; }
    public void OutputRuntimeSettings();
}
