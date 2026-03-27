namespace AndrewM5.DevKit.Logging.Abstractions.Options;

public class LogFlushServiceSettings
{
    public bool CreateLogFile { get; set; } = false;
    public string LogFilePath { get; set; } = string.Empty;
    public int MaxBufferCount { get; set; } = 50;
    public int FlushIntervalSeconds { get; set; } = 5;
    public bool AllowCreateFileInContainer { get; set; } = false;

    public LogFlushServiceSettings Clone()
    {
        return new LogFlushServiceSettings
        {
            CreateLogFile = CreateLogFile,
            LogFilePath = LogFilePath,
            MaxBufferCount = MaxBufferCount,
            FlushIntervalSeconds = FlushIntervalSeconds,
            AllowCreateFileInContainer = AllowCreateFileInContainer,
        };
    }
}
