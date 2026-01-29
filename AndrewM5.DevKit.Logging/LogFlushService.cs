using AndrewM5.DevKit.Core;
using AndrewM5.DevKit.Logging.Abstractions;
using AndrewM5.DevKit.Logging.Settings;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace AndrewM5.DevKit.Logging;

public class LogFlushService : BackgroundService, ILogFlushService
{
    public LogFlushServiceSettings RuntimeSettings { get; private set; }

    private readonly bool _runningInContainer;

    public LogFlushService(IOptions<LogFlushServiceSettings> settings)
    {
        if (settings == null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        RuntimeSettings = settings.Value.Clone();
        _runningInContainer = Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true";
    }

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        DateTime lastFlushTime = DateTime.UtcNow;

        while (!cancellationToken.IsCancellationRequested)
        {
            TimeSpan elapsedTime = DateTime.UtcNow - lastFlushTime;

            if (LogRegistry.GetLogFileQueueCount() >= RuntimeSettings.MaxBufferCount || elapsedTime.TotalSeconds >= RuntimeSettings.FlushIntervalSeconds)
            {
                FlushBuffer();
                lastFlushTime = DateTime.UtcNow;
            }

            await Task.Delay(100, cancellationToken); // Short delay to avoid busy while loop
        }

        FlushBuffer(); //Final flush when task is canceled.
    }

    private void FlushBuffer()
    {
        try
        {
            if (!RuntimeSettings.CreateLogFile)
            {
                return;
            }

            var messages = LogRegistry.DequeueFromLogFileBuffer();
            if (messages.Length == 0)
            {
                return;
            }

            StringBuilder stringBuilder = new StringBuilder();
            foreach (var msg in messages)
            {
                stringBuilder.AppendLine(msg);
            }

            string output = stringBuilder.ToString();

            bool allowCreateFile = !_runningInContainer || RuntimeSettings.AllowCreateFileInContainer;

            if (allowCreateFile && !string.IsNullOrWhiteSpace(RuntimeSettings.LogFilePath))
            {
                var writeToFile = FileExtension.WriteToFile(RuntimeSettings.LogFilePath, output, true);
                if (!writeToFile.MethodSuccess)
                {
                    throw writeToFile.Exception;
                }
            }
        }
        catch (Exception ex) 
        {
            Debug.WriteLine(LogFormatter.Format(true, nameof(LogFlushService),"Failed to flush logs", LogLevel.Error, ex));
        }
    }

    public void DisplayRuntimeSettings()
    {
        Console.WriteLine($"--- Log Flush Service Settings ---");

        Type type = RuntimeSettings.GetType();
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            object? value = property.GetValue(RuntimeSettings);
            Console.WriteLine($"  {property.Name}: {value}");
        }
    }
}
