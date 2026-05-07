# Custom Logging Module: Quick Start

The Custom Logger provides a flexible way to manage diagnostic messages across the application. It supports hierarchical logging levels, console output toggling, 
and global manager access via the LoggingHost.

## 1. Access and Configuration 

The LoggerManager is the central hub for all logging activities. It is responsible for managing each custom logger and can be accessed through the LoggingHost.
```
LoggingHost.LoggerManager;
```

The behavior of the LoggerManager is driven by the appsettings.json file. 

```
"CustomLogger": {
    "OutputLogLevel": "Debug",
    "FileOutputLogLevel": "Warning"
},
```




// Display current runtime configurations
_loggerManager.LogRuntimeSettings();
2. Creating a Logger InstanceYou can retrieve or create a specific logger by providing a category name. This allows you to filter logs by component later.C#var logger = _loggerManager.GetLogger("TestLogger");
3. Logging by SeverityThe library supports standard log levels. By default, these are directed to the Visual Studio Output Panel (Debug/Trace).MethodDescriptionLogTraceFine-grained informational events.LogDebugFine-grained events useful to debug an application.LogInformationProgress of the application at a high level.LogWarningPotentially harmful situations.LogErrorError events that might still allow the app to run.LogCriticalSevere error events that will presumably lead the app to abort.4. Console Output ControlYou can dynamically toggle whether a specific logger mirrors its output to the standard Console.C#// Enable console output
logger.EnableConsoleOutput();
logger.LogInformation("This will appear in the Console window.");

// Disable console output
logger.DisableConsoleOutput();
logger.LogInformation("This will only appear in the Debug/Output panel.");
5. Enabling and Disabling LoggersIf you need to silence a specific component without removing the code, you can disable the logger instance entirely.C#// The logger is active by default
logger.LogInformation("Logger is active.");

// Kill all output for this instance
logger.DisableLogger();
logger.LogInformation("This message will be ignored.");
Implementation ExampleTo see these features in action, you can refer to the TestCustomLogger() method in our internal test suite:

# Logging Module: Log Flusher

The **Log Flusher** is an optional add-on service that provides persistence for your logs. While the standard logger outputs to the Debug window or Console, 
the Flusher ensures that log data is periodically captured and written to a physical file on disk.

## 1. Accessing and Configurating the Flush Service
Similar to the Logger Manager, the Flush Service is managed via its own host provider.
Before attempting to write to disk, the service checks the CreateLogFile runtime setting. If this is disabled, the service will remain idle to save system resources.


3. Persistent Logging Workflow
Once the service is active, any message logged through a standard ILogger instance is intercepted and queued for the background flushing process.

C#
var logger = _loggerManager.GetLogger("TestLogFlusher");

// The Flusher monitors these calls automatically
for (int i = 0; i < 100; i++)
{
    logger.LogInformation($"Log message {i}");
    await Task.Delay(100); 
}
4. Output Location
The log file destination is determined by the runtime settings. You can programmatically verify the output path:

C#
if (_logFlushService.RuntimeSettings.CreateLogFile)
{
    Console.WriteLine($"Log file created at: {_logFlushService.RuntimeSettings.LogFilePath}");
}



Configuration (appsettings.json)The behavior of the Custom Logger and the Log Flusher is driven by the appsettings.json file. 

Below is the standard schema and a breakdown of what each property controls.JSON{
  "CustomLogger": {
    "OutputLogLevel": "Debug",
    "FileOutputLogLevel": "Warning"
  },
  "CustomLoggerFlusher": {
    "CreateLogFile": false,
    "AllowCreateFileInContainer": true,
    "MaxBufferCount": 50,
    "FlushIntervalSeconds": 2,
    "LogFilePath": "C:\\Logs\\app_log.txt"
  }
}
1. Custom Logger SettingsThese settings define the verbosity of your logs across different outputs.PropertyDescriptionOutputLogLevelSets the minimum level for logs appearing in the IDE/Console (e.g., Debug, Information).FileOutputLogLevelSets the minimum level for logs captured by the Flusher (e.g., Warning). This allows you to see everything in the console while only saving critical data to disk.2. Custom Logger Flusher SettingsThese settings control the performance and behavior of the file-writing engine.CreateLogFile: A master toggle. If false, the flusher service will not initialize a file stream.  AllowCreateFileInContainer: When set to true, it allows the library to attempt file creation even when detecting a Docker or Linux container environment.MaxBufferCount: The number of log entries held in memory before a forced flush to disk occurs.  FlushIntervalSeconds: How often (in seconds) the service checks the buffer to write to the file, regardless of whether the MaxBufferCount has been reached.  LogFilePath: The absolute path where the log file will be generated.  