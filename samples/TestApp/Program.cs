/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.Core;
using Integration.DevKit.Core.Configuration;
using Integration.DevKit.CredentialMgmt;
using Integration.DevKit.CustomLogger;
using Integration.DevKit.CustomLogger.Flusher;
using Integration.DevKit.ProcessLauncher;
using Integration.DevKit.RESTApiMgmt;
using Integration.DevKit.RESTApiMgmt.Contracts;
using Integration.DevKit.SQLMgmt;
using Integration.DevKit.TaskMgmt;
using Integration.DevKit.TaskMgmt.Contracts;
using Integration.DevKit.ThreadLocks;
using Integration.DevKit.ThreadSafeItems;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Diagnostics;

namespace TestApp;

public class Program
{
    public static async Task Main(string[] args)
    {
        var cryptoContract = new ConfigProtectorContract('|')
        {
            Signature = "ENC",
            Version = "v1"
        };
        var base64Protector = new Base64ConfigProtector();
        var aesProtector = new AesConfigProtector("my-super-secret-32-byte-long-key!!", "1234567890123456");

        var configBuilder = new ConfigurationBuilder().AddJsonFile("appsettings.json", false);

        var encryptedBuild = configBuilder.EncryptJsonOnBuild(
            cryptoContract,
            (options) =>
            {
                options.Encrypt("Integration.DevKit:RnadomManagement");
                options.Encrypt("Integration.DevKit:SQLManagement:Clients:TestClient:ConnectionString", aesProtector);
            }
        ).Build();
        var decryptedConfig = configBuilder.DecryptJsonOnBuild(cryptoContract, new List<IConfigProtector> { base64Protector, aesProtector }).Build();

        var builder = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddCustomLogging(decryptedConfig);
                services.AddCustomLogFlusher(decryptedConfig);
                services.AddProcessLauncher();
                services.AddRESTApiMgmt(decryptedConfig);
                services.AddFileSecretStore("TestApp", "C:\\Users\\andre\\Projects\\Junk\\Secrets", "C:\\Users\\andre\\Projects\\Junk\\Keys");
                services.AddThreadLocks();
                services.AddThreadSafeItems();
                //services.AddTaskMgmt(decryptedConfig);
                services.AddSQLMgmt(decryptedConfig);

                // Register your app entry
                services.AddSingleton<AppEntry>();
            });

        //Service_CustomLogger.AddCustomLogging_OnDemand(decryptedConfig);
        //Service_CustomLogFlusher.AddCustomLogFlusher_OnDemand(decryptedConfig);
        //Service_ProcessLauncher.AddProcessLauncher_OnDemand();
        //Service_RESTApiMgmt.AddRESTApiMgmt_OnDemand(decryptedConfig);
        //Service_ThreadLocks.AddThreadLocks_OnDemand();
        //Service_ThreadSafeItems.AddThreadSafeItems_OnDemand();
        //Service_TaskMgmt.AddTaskMgmt_OnDemand(decryptedConfig);
        //Service_SQLMgmt.AddSQLMgmt_OnDemand(decryptedConfig);
        //Service_CredentialMgmt.AddFileSecretStore_OnDemand("TestApp", "C:\\Users\\andre\\Projects\\Junk\\Secrets", "C:\\Users\\andre\\Projects\\Junk\\Keys");

        var app = builder.Build();

        //var serviceProvider = app.Services;
        var serviceProvider = OnDemand_Registry.GetServiceProvider(decryptedConfig);

        var lifetime = app.Services.GetRequiredService<IHostApplicationLifetime>();

        Service_CustomLogger.Initialize(serviceProvider);
        Service_CustomLogFlusher.Initialize(serviceProvider);
        Service_ProcessLauncher.Initialize(serviceProvider);
        Service_RESTApiMgmt.Initialize(serviceProvider);
        Service_ThreadLocks.Initialize(serviceProvider);
        Service_ThreadSafeItems.Initialize(serviceProvider);
        Service_TaskMgmt.Initialize(serviceProvider);
        Service_SQLMgmt.Initialize(serviceProvider);
        Service_CredentialMgmt.InitializeFileSecretStore(serviceProvider);

        // 1. Fire up the background workers (LogFlusher, etc.)
        await app.StartAsync();

        // 2. Fetch and run your custom console logic (blocks until your work is done)
        var entry = app.Services.GetRequiredService<AppEntry>();
        await entry.RunAsync(args);

        // 3. Gracefully stop the background workers once your main work finishes
        await app.StopAsync();
    }
}

public class AppEntry
{

    public async Task RunAsync(string[] args)
    {
        //await TestCoreClasses();
        //TestCustomLogger();
        await TestCustomLoggerFlusher();
        //await TestProcessLauncher();
        //await TestApiManagement();
        //TestCredentialManagement();
        //await TestThreadSafeItems();
        //await TestTaskManagement();
        //await TestTaskManagement_SyncManagedTask();
        //await TestTaskManagement_AsyncManagedTask();
        //await AdhocTesting();

        //await TestApiManagementCredentials(true, false);

        Console.WriteLine("Press enter to exit");
        Console.ReadLine();
    }

    private async Task AdhocTesting()
    {
        var strategy_7AM_Settings = new TimeStrategySettings
        {
            SkipFirstIterationWait = true,
            FastForwardToPresent = true,
            CustomStartTime = new TimeSpan(7, 0, 0)
        };
        var strategy = new TimeStrategy_Interval(new TimeSpan(0,2,0) ,strategy_7AM_Settings);

        var taskSettings = new ManagedTaskSettings
        {
            IterationStrategy = strategy,
            MaxIterations = -1
        };

        var startTask = await Service_TaskMgmt.TaskManager.StartTask(new SimpleShortTask(), TaskExecutionMode.Asyncronous, taskSettings);
        if (!startTask.MethodSuccess)
        {
            throw startTask.Exception;
        }

        await startTask.Result.RunningTask!;
    }

    private void TestCustomLogger()
    {
        Console.WriteLine("----|----|Custom Logger Management|----|----");
        Console.WriteLine("Custom Logger Test (Look at Visual Studio output panel)");

        var _loggerManager = Service_CustomLogger.LoggerManager;

        _loggerManager.LogRuntimeSettings();

        var logger = _loggerManager.GetLogger("TestLogger");

        logger.LogTrace("This is a trace");
        logger.LogDebug("This is debug");
        logger.LogInformation("This is information!");
        logger.LogWarning("This is a warning!");
        logger.LogError("This is an error!");
        logger.LogCritical("This is critical");

        logger.EnableConsoleOutput();

        logger.LogInformation("This should show in the console.");

        logger.DisableConsoleOutput();

        logger.LogInformation("This should not show in the console.");

        logger.LogInformation("This logger is enabled");

        logger.DisableLogger();

        logger.LogInformation("This logger is disabled");
    }
    private async Task TestCustomLoggerFlusher()
    {
        Console.WriteLine("----|----|Custom Logger Flusher|----|----");
        Console.WriteLine("Custom Logger Flusher Test (Look at Visual Studio output panel)");

        var _loggerManager = Service_CustomLogger.LoggerManager;
        var _logFlushService = Service_CustomLogFlusher.LogFlushService;

        _loggerManager.LogRuntimeSettings();
        _logFlushService.LogRuntimeSettings();

        var logger = _loggerManager.GetLogger("TestLogFlusher");

        if (!_logFlushService.RuntimeSettings.CreateLogFile)
        {
            Console.WriteLine($"{nameof(_logFlushService.RuntimeSettings.CreateLogFile)} set to false, returning...");

            return;
        }

        for (int i = 0; i < 100; i++)
        {
            logger.LogInformation($"Log message {i}");

            await Task.Delay(100);
        }

        if (_logFlushService.RuntimeSettings.CreateLogFile)
        {
            Console.WriteLine($"A log file should have now been created at {_logFlushService.RuntimeSettings.LogFilePath}");
        }
    }
    private async Task TestProcessLauncher()
    {
        Console.WriteLine("----|----|Process Launcher|----|----");
        var _processManager = Service_ProcessLauncher.ProcessManager;

        Console.WriteLine("Console Command Ping Test (Limited)");
        var processConfig_Limited = new ManagedProcessConfig
        {
            ProcessKey = "PingTest",
            Command = "cmd.exe",
            Arguments = "/c ping 127.0.0.1 -n 4",
            ShowWindow = true,
            TimeoutSeconds = 10,
            WorkingDirectory = Environment.CurrentDirectory
        };

        var startResult = _processManager.StartProcess(processConfig_Limited);
        if (!startResult.MethodSuccess)
        {
            Console.WriteLine($"Failed to start process: {startResult.Exception}");
            return;
        }

        await _processManager.WaitForExitAsync(startResult.Result.Process);

        Console.WriteLine("Console Command Ping Test");
        var processConfig = new ManagedProcessConfig
        {
            ProcessKey = "PingTest",
            Command = "cmd.exe",
            Arguments = "/c ping 127.0.0.1 -t",
            ShowWindow = true,
            TimeoutSeconds = 10,
            WorkingDirectory = Environment.CurrentDirectory
        };

        var startResult1 = _processManager.StartProcess(processConfig);
        if (!startResult1.MethodSuccess)
        {
            Console.WriteLine($"Failed to start process: {startResult1.Exception}");
            return;
        }

        await Task.Delay(3000);

        Console.WriteLine("Canceling Console Command Ping Test");
        startResult.Result.Cancel();

        await _processManager.WaitForExitAsync(startResult1.Result.Process);
    }
    private async Task TestApiManagement()
    {
        Console.WriteLine("----|----|Api Management|----|----");
        var _apiManager = Service_RESTApiMgmt.ApiManager;
        var _client = _apiManager.GetClient("TestClient");

        _apiManager.LogRuntimeSettings();

        ApiEndpoint Posts = new ApiEndpoint("/Posts");

        Console.WriteLine("GET Posts: ");
        var getAll = await _client.GetAsync(Posts.BuildUrl());
        if (!getAll.MethodSuccess)
        {
            Console.WriteLine(getAll.Exception.Message);
            return;
        }

        Console.WriteLine(getAll.Result);
        Console.WriteLine("----------------------------------------------------");
        Console.WriteLine("GET Post: ");

        var buildUrlGET = Posts.BuildPositionalUrl(new List<object> { 1 });
        if (!buildUrlGET.MethodSuccess)
        {
            Console.WriteLine(buildUrlGET.Exception.Message);
            return;
        }

        var getSingle = await _client.GetAsync(buildUrlGET.Result);
        if (!getSingle.MethodSuccess)
        {
            Console.WriteLine(getSingle.Exception.Message);
            return;
        }

        Console.WriteLine(getSingle.Result);
        Console.WriteLine("----------------------------------------------------");
        Console.WriteLine("POST Post: ");

        var getJsonPOST = JsonUtils.SerializeObjectToJson(new Dictionary<string, object>
        {
            ["title"] = "foo",
            ["body"] = "bar",
            ["userId"] = 1
        });
        if (!getJsonPOST.MethodSuccess)
        {
            Console.WriteLine(getJsonPOST.Exception.Message);
            return;
        }

        var createContentPOST = _client.CreateHttpContent(RESTApiMediaTypes.Json, getJsonPOST.Result);
        if (!createContentPOST.MethodSuccess)
        {
            Console.WriteLine(createContentPOST.Exception.Message);
            return;
        }

        var post = await _client.PostAsync(Posts.BuildUrl(), createContentPOST.Result);
        if (!post.MethodSuccess)
        {
            Console.WriteLine(post.Exception.Message);
            return;
        }

        Console.WriteLine(post.Result);
        Console.WriteLine("----------------------------------------------------");
        Console.WriteLine("PUT Post: ");

        var getJsonPut = JsonUtils.SerializeObjectToJson(new Dictionary<string, object>
        {
            ["id"] = 1,
            ["title"] = "updated title",
            ["body"] = "updated body",
            ["userId"] = 1
        });
        if (!getJsonPut.MethodSuccess)
        {
            Console.WriteLine(getJsonPut.Exception.Message);
            return;
        }

        var createContentPUT = _client.CreateHttpContent(RESTApiMediaTypes.Json, getJsonPut.Result);
        if (!createContentPUT.MethodSuccess)
        {
            Console.WriteLine(createContentPUT.Exception.Message);
            return;
        }

        var buildUrlPUT = Posts.BuildPositionalUrl(new List<object> { 1 });
        if (!buildUrlPUT.MethodSuccess)
        {
            Console.WriteLine(buildUrlPUT.Exception.Message);
            return;
        }

        var put = await _client.PutAsync(buildUrlPUT.Result, createContentPUT.Result);
        if (!put.MethodSuccess)
        {
            Console.WriteLine(put.Exception.Message);
            return;
        }

        Console.WriteLine(put.Result);
        Console.WriteLine("----------------------------------------------------");
        Console.WriteLine("DELETE Post: ");

        var buildUrlDELETE = Posts.BuildPositionalUrl(new List<object> { 1 });
        if (!buildUrlDELETE.MethodSuccess)
        {
            Console.WriteLine(buildUrlDELETE.Exception.Message);
            return;
        }

        var delete = await _client.DeleteAsync(buildUrlDELETE.Result);
        if (!delete.MethodSuccess)
        {
            Console.WriteLine(delete.Exception.Message);
            return;
        }

        Console.WriteLine(delete.Result);
        Console.WriteLine("----------------------------------------------------");
        Console.WriteLine("Failure Test: ");

        var result = await _client.GetAsync("/this-route-does-not-exist");
        if (!result.MethodSuccess)
        {
            Console.WriteLine(result.Exception.Message);
        }

        Console.WriteLine("----------------------------------------------------");
        Console.WriteLine("Client Metrics: ");
        Console.WriteLine(_client.ClientMetrics.ToString());
    }
    private void TestCredentialManagement()
    {
        Console.WriteLine("----|----|Credential Management|----|----");
        var store = Service_CredentialMgmt.FileSecretStore;

        string fileName = "Api";
        string initialKey = "ApiKey";
        string initialValue = "super-secret-value";
        // Set a key
        var setResult = store.SetKey(fileName, initialKey, initialValue);
        if (setResult.MethodSuccess)
        {
            Console.WriteLine($"Set value: {initialValue}");
        }
        else
        {
            Console.WriteLine($"Set failed: {setResult.Exception}");
            return;
        }

        // Get a key
        var getResult = store.GetKey(fileName, initialKey);
        if (getResult.MethodSuccess)
        {
            Console.WriteLine($"Retrieved value: {getResult.Result}");
        }
        else
        {
            Console.WriteLine($"Get failed: {getResult.Exception}");
        }

        // Delete the key
        var deleteKeyResult = store.DeleteKey(fileName, initialKey);
        if (deleteKeyResult.MethodSuccess)
        {
            Console.WriteLine($"Deleted file {fileName}");
        }
        else
        {
            Console.WriteLine($"Delete failed: {deleteKeyResult.Exception}");
        }

        Console.WriteLine("Key deleted successfully.");

        // Verify deletion
        var verifyResult = store.GetKey(fileName, initialKey);
        if (!verifyResult.MethodSuccess)
        {
            Console.WriteLine("Verified: key no longer exists.");
        }
        else
        {
            Console.WriteLine("DeleteKey failed — key still exists.");
        }

        // Delete secret file
        var deleteResult = store.DeleteSecret(fileName);
        if (deleteResult.MethodSuccess)
        {
            Console.WriteLine($"Deleted file {fileName}");
        }
        else
        {
            Console.WriteLine($"Delete failed: {deleteResult.Exception}");
        }
    }
    private async Task TestThreadSafeItems()
    {
        Console.WriteLine("----|----|Thread Safe FileIO |----|----");

        var _threadSafeFileIO = Service_ThreadSafeItems.ThreadSafeFileIOClass;
        string filePath = "C:\\Users\\andre\\Projects\\Junk\\ThreadSafeTest.txt";
        string filePath_AsyncLock = "C:\\Users\\andre\\Projects\\Junk\\ThreadSafeTest_AsyncLock.txt";
        string filePath_SyncLock = "C:\\Users\\andre\\Projects\\Junk\\ThreadSafeTest_SyncLock.txt";
        string filePath_HybridLock = "C:\\Users\\andre\\Projects\\Junk\\ThreadSafeTest_HybridLock.txt";

        NullOperationResult writeFile;
        OperationResult<string[]> readFile;

        Console.WriteLine("==Test 1: Control==");
        Console.WriteLine("FilePath: " + filePath);
        
        Console.WriteLine("Deleting File...");
        var deleteFile = FileUtils.DeleteFile(filePath);
        if (!deleteFile.MethodSuccess)
        {
            Console.WriteLine(deleteFile.Exception);
        }

        Console.WriteLine("File Exists: " + FileUtils.DoesFileExist(filePath));

        Console.WriteLine("\n==Test 2: Create File==");
        writeFile = await _threadSafeFileIO.WriteToFileAsync(filePath, "This is a async test message");
        if (!writeFile.MethodSuccess)
        {
            Console.WriteLine(writeFile.Exception);
        }

        Console.WriteLine("File Exists: " + FileUtils.DoesFileExist(filePath));

        readFile = await _threadSafeFileIO.ReadFileLinesAsync(filePath);
        if (!readFile.MethodSuccess)
        {
            Console.WriteLine(readFile.Exception);
        }

        Console.WriteLine("Read Result: ");
        Console.WriteLine(string.Join("\r\n", readFile.Result));

        Console.WriteLine("\n==Test 3: Write File (append)==");
        writeFile = await _threadSafeFileIO.WriteToFileAsync(filePath, "This is a async test message with appending", true);
        if (!writeFile.MethodSuccess)
        {
            Console.WriteLine(writeFile.Exception);
        }

        Console.WriteLine("File Exists: " + FileUtils.DoesFileExist(filePath));

        readFile = await _threadSafeFileIO.ReadFileLinesAsync(filePath);
        if (!readFile.MethodSuccess)
        {
            Console.WriteLine(readFile.Exception);
        }

        Console.WriteLine("Read Result: ");
        Console.WriteLine(string.Join("\r\n", readFile.Result));

        Console.WriteLine("\n==Test 4: Write File (overwrite)==");
        writeFile = await _threadSafeFileIO.WriteToFileAsync(filePath, "This is a async test message with overwrite");
        if (!writeFile.MethodSuccess)
        {
            Console.WriteLine(writeFile.Exception);
        }

        Console.WriteLine("File Exists: " + FileUtils.DoesFileExist(filePath));

        readFile = await _threadSafeFileIO.ReadFileLinesAsync(filePath);
        if (!readFile.MethodSuccess)
        {
            Console.WriteLine(readFile.Exception);
        }

        Console.WriteLine("Read Result: ");
        Console.WriteLine(string.Join("\r\n", readFile.Result));

        Console.WriteLine("\n==Test 5: Write File (multi-line)==");
        writeFile = await _threadSafeFileIO.WriteToFileAsync(filePath, new string[] { "This is a async test message", "This is a async test message 1" });
        if (!writeFile.MethodSuccess)
        {
            Console.WriteLine(writeFile.Exception);
        }

        Console.WriteLine("File Exists: " + FileUtils.DoesFileExist(filePath));

        readFile = await _threadSafeFileIO.ReadFileLinesAsync(filePath);
        if (!readFile.MethodSuccess)
        {
            Console.WriteLine(readFile.Exception);
        }

        Console.WriteLine("Read Result: ");
        Console.WriteLine(string.Join("\r\n", readFile.Result));

        Console.WriteLine("\n==Test 6: Async-Only Contention (Race Condition)==");

        // Clean start
        FileUtils.DeleteFile(filePath_AsyncLock);

        int numberOfThreads = 10;
        int writesPerThread = 20;
        var tasks = new List<Task<NullOperationResult>>();

        Console.WriteLine($"Starting {numberOfThreads} parallel tasks, each writing {writesPerThread} times...");

        for (int i = 0; i < numberOfThreads; i++)
        {
            int threadId = i; // Capture local variable for the closure
            tasks.Add(Task.Run(async () =>
            {
                NullOperationResult lastResult = new NullOperationResult().SetMethodSuccess();

                for (int j = 0; j < writesPerThread; j++)
                {
                    var writeResult = await _threadSafeFileIO.WriteToFileAsync(
                        filePath_AsyncLock,
                        $"Thread {threadId} - Entry {j}",
                        append: true,
                        lockTimeoutMs: 10000 // Higher timeout to handle the queue
                    );

                    if (!writeResult.MethodSuccess) return writeResult;
                    lastResult = writeResult;
                }
                return lastResult;
            }));
        }

        // Wait for all "threads" to finish
        var results = await Task.WhenAll(tasks);

        // Check for failures
        int failureCount = results.Count(r => !r.MethodSuccess);
        if (failureCount > 0)
        {
            Console.WriteLine($"FAILURE: {failureCount} operations failed during contention!");
            foreach (var fail in results.Where(r => !r.MethodSuccess))
                Console.WriteLine($" -> {fail.Exception.Message}");
        }
        else
        {
            Console.WriteLine("SUCCESS: All parallel writes completed without file-in-use errors.");
        }

        // Final Verification: Count the lines in the file
        var finalRead = await _threadSafeFileIO.ReadFileLinesAsync(filePath_AsyncLock);
        int expectedLines = numberOfThreads * writesPerThread;

        if (finalRead.MethodSuccess && finalRead.Result.Length == expectedLines)
        {
            Console.WriteLine($"INTEGRITY PASSED: Found {finalRead.Result.Length} lines as expected.");
        }
        else
        {
            Console.WriteLine($"INTEGRITY FAILED: Expected {expectedLines} lines, but found {finalRead.Result?.Length ?? 0}.");
        }

        Console.WriteLine("\n==Test 7: Sync-Only Contention (Parallel.For)==");

        FileUtils.DeleteFile(filePath_SyncLock);

        int totalWrites = 50;

        // Parallel.For uses the thread pool to slam the sync methods
        Parallel.For(0, totalWrites, i =>
        {
            _threadSafeFileIO.WriteToFile(filePath_SyncLock, $"Sync-Write-{i}", true);
        });

        var finalRead_sync = _threadSafeFileIO.ReadFileLines(filePath_SyncLock);

        if (finalRead.MethodSuccess && finalRead_sync.Result.Length == totalWrites)
        {
            Console.WriteLine($"SYNC SUCCESS: {finalRead_sync.Result.Length}/{totalWrites} lines written.");
        }
        else
        {
            Console.WriteLine("SYNC FAILURE: Monitor lock failed to prevent collision.");
        }

        Console.WriteLine("\n==Test 8: Hybrid (Sync + Async) Contention==");
        Console.WriteLine("NOTE: This should fail and produce exceptions since you should not combine Sync and Async locking");

        FileUtils.DeleteFile(filePath_HybridLock);

        int iterations = 15;

        // Task for Async writes
        var asyncTask = Task.Run(async () =>
        {
            for (int i = 0; i < iterations; i++)
            {
                await _threadSafeFileIO.WriteToFileAsync(filePath_HybridLock, $"Async-Write-{i}", true);
            }
        });

        // Task for Sync writes
        var syncTask = Task.Run(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                _threadSafeFileIO.WriteToFile(filePath_HybridLock, $"Sync-Write-{i}", true);
            }
        });

        await Task.WhenAll(asyncTask, syncTask);

        var finalRead_hybrid = await _threadSafeFileIO.ReadFileLinesAsync(filePath_HybridLock);
        int expectedLines_hybrid = iterations * 2;

        if (finalRead.MethodSuccess && finalRead_hybrid.Result.Length == expectedLines_hybrid)
        {
            Console.WriteLine($"HYBRID SUCCESS: {finalRead_hybrid.Result.Length} total lines written.");
        }
        else
        {
            Console.WriteLine("HYBRID COLLISION: Some writes were lost or an exception occurred.");
            // If this fails, it means your IThreadLockManager uses different 
            // locks for Sync vs Async internally.
        }
    }
    private async Task TestTaskManagement()
    {
        var _taskManager = Service_TaskMgmt.TaskManager;
        var _taskRegistry = Service_TaskMgmt.TaskRegistry;
        _taskManager.LogRuntimeSettings();

        OperationResult<IManagedTaskHandle> createTask;
        var settings = new ManagedTaskSettings();

        Console.WriteLine("\n==Test 1: Start Single Task==");
        Console.WriteLine("NOTE: Look at output logs");
        createTask = await _taskManager.StartTask(new SimpleLongTask(), TaskExecutionMode.Asyncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        Console.WriteLine("waiting 2 seconds for next task");
        await Task.Delay(2000);

        Console.WriteLine("\n==Test 2: Cancel Single Task==");
        Console.WriteLine("NOTE: Look at output logs");
        createTask.Result.Cancel();

        Console.WriteLine("waiting for task to finish");
        await createTask.Result.RunningTask!;
        Console.WriteLine("taks finished");

        settings = new ManagedTaskSettings();

        Console.WriteLine("\n==Test 3: Start Single Task==");
        Console.WriteLine("NOTE: Look at output logs");
        createTask = await _taskManager.StartTask(new SimpleLongTask_NoTokenChecking(), TaskExecutionMode.Asyncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        Console.WriteLine("waiting 2 seconds for next task");
        await Task.Delay(2000);

        Console.WriteLine("\n==Test 4: Cancel Single Task==");
        Console.WriteLine("This task does not check if the cancelation token is canceled so this will continue to run even after a cancel. ");
        Console.WriteLine("However the RunningTask will be canceled so you wont get stuck waiting for something that wont cancel");
        Console.WriteLine("NOTE: Look at output logs");
        createTask.Result.Cancel();

        Console.WriteLine("waiting for task to finish");
        await createTask.Result.RunningTask!;
        Console.WriteLine("task finished");

        settings = new ManagedTaskSettings();

        Console.WriteLine("\n==Test 5: Start Task with Timeout==");
        Console.WriteLine("NOTE: Look at output logs");
        settings.Timeout = TimeSpan.FromSeconds(3);

        createTask = await _taskManager.StartTask(new SimpleLongTask(), TaskExecutionMode.Asyncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        Console.WriteLine("waiting for task to finish");
        await createTask.Result.RunningTask!;
        Console.WriteLine("task finished");

        settings = new ManagedTaskSettings();
    }
    private async Task TestTaskManagement_SyncManagedTask()
    {
        Console.WriteLine("----|----|Task Management|----|----");

        var _taskManager = Service_TaskMgmt.TaskManager;
        var _taskRegistry = Service_TaskMgmt.TaskRegistry;
        _taskManager.LogRuntimeSettings();

        OperationResult<IManagedTaskHandle> createTask;
        var settings = new ManagedTaskSettings();
        var strategySettings = new TimeStrategySettings();

        Console.WriteLine("\n==Test 1: Synchronous (Default Settings)==");
        createTask = await _taskManager.StartTask(new SimpleShortTask(), TaskExecutionMode.Syncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        settings = new ManagedTaskSettings();

        Console.WriteLine("\n==Test 2: Synchronous (Custom Settings)==");
        Console.WriteLine("NOTE: Look at output logs");
        settings.MaxIterations = 2;

        Console.WriteLine("Changed Interation Settings");
        Console.WriteLine("  MaxIterations = 2");

        createTask = await _taskManager.StartTask(new SimpleShortTask(), TaskExecutionMode.Syncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        settings = new ManagedTaskSettings();

        Console.WriteLine("\n==Test 3: Synchronous (Custom Settings)==");
        Console.WriteLine("NOTE: Look at output logs");
        settings.MaxIterations = 2;
        settings.StopIteratingOnException = false;

        Console.WriteLine("Changed Interation Settings");
        Console.WriteLine("  MaxIterations = 2");
        Console.WriteLine("  StopIteratingOnException = false");

        createTask = await _taskManager.StartTask(new SimpleBrokenTask(), TaskExecutionMode.Syncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        settings = new ManagedTaskSettings();

        Console.WriteLine("\n==Test 4: Synchronous (Custom Settings)==");
        Console.WriteLine("NOTE: Look at output logs");
        settings.RetryOnException = true;
        settings.MaxRetryCount = 3;

        Console.WriteLine("Changed Interation Settings");
        Console.WriteLine("  RetryOnException = true");
        Console.WriteLine("  MaxRetryCount = 3");

        createTask = await _taskManager.StartTask(new SimpleBrokenTask(), TaskExecutionMode.Syncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        settings = new ManagedTaskSettings();

        Console.WriteLine("\n==Test 5: Synchronous (Custom Settings)==");
        Console.WriteLine("NOTE: Look at output logs");
        settings.MaxIterations = 2;
        settings.RetryOnException = true;
        settings.MaxRetryCount = 3;
        settings.StopIterationAfterMaxRetries = false;

        Console.WriteLine("Changed Interation Settings");
        Console.WriteLine("  MaxIterations = 2");
        Console.WriteLine("  RetryOnException = true");
        Console.WriteLine("  MaxRetryCount = 3");
        Console.WriteLine("  StopIterationAfterMaxRetries = false");

        createTask = await _taskManager.StartTask(new SimpleBrokenTask(), TaskExecutionMode.Syncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        settings = new ManagedTaskSettings();

        Console.WriteLine("\n==Test 6: Synchronous (Custom Settings)==");
        Console.WriteLine("NOTE: Look at output logs");
        settings.MaxIterations = 2;
        settings.IterationStrategy = new TimeStrategy_Interval(TimeSpan.FromSeconds(10), strategySettings);

        Console.WriteLine("Changed Interation Settings");
        Console.WriteLine("  MaxIterations = 2");
        Console.WriteLine("  IterationStrategy = Interval (Every 10 seconds)");

        createTask = await _taskManager.StartTask(new SimpleShortTask(), TaskExecutionMode.Syncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        settings = new ManagedTaskSettings();
        strategySettings = new TimeStrategySettings();

        Console.WriteLine("\n==Test 7: Synchronous (Custom Settings)==");
        Console.WriteLine("NOTE: Look at output logs");
        settings.MaxIterations = 2;

        strategySettings.SkipFirstIterationWait = false;

        settings.IterationStrategy = new TimeStrategy_Interval(TimeSpan.FromSeconds(10), strategySettings);

        Console.WriteLine("Changed Interation Settings");
        Console.WriteLine("  MaxIterations = 2");
        Console.WriteLine("  IterationStrategy = Interval (Every 10 seconds)");
        Console.WriteLine("Changed Strategy Settings");
        Console.WriteLine("  SkipFirstIterationWait = false");

        createTask = await _taskManager.StartTask(new SimpleShortTask(), TaskExecutionMode.Syncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        settings = new ManagedTaskSettings();
        strategySettings = new TimeStrategySettings();

        Console.WriteLine("\n==Test 8: Synchronous (Custom Settings)==");
        Console.WriteLine("NOTE: Look at output logs");
        settings.MaxIterations = 2;

        strategySettings.CustomStartDate = DateOnly.FromDateTime(DateTime.Now);
        strategySettings.CustomStartTime = DateTime.Now.TimeOfDay.Add(TimeSpan.FromSeconds(-15));

        settings.IterationStrategy = new TimeStrategy_Interval(TimeSpan.FromSeconds(10), strategySettings);

        Console.WriteLine("Changed Interation Settings");
        Console.WriteLine("  MaxIterations = 2");
        Console.WriteLine("  IterationStrategy = Interval (Every 10 seconds)");
        Console.WriteLine("Changed Strategy Settings");
        Console.WriteLine($"  CustomStartDate = {strategySettings.CustomStartDate}");
        Console.WriteLine($"  CustomStartTime = {strategySettings.CustomStartTime}");
        Console.WriteLine("NOTE: This will look like its not abiding the custom start date/time but it is. Its just catching up to the current time");

        createTask = await _taskManager.StartTask(new SimpleShortTask(), TaskExecutionMode.Syncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        settings = new ManagedTaskSettings();
        strategySettings = new TimeStrategySettings();

        Console.WriteLine("\n==Test 9: Synchronous (Custom Settings)==");
        Console.WriteLine("NOTE: Look at output logs");
        settings.MaxIterations = 2;

        strategySettings.CustomStartDate = DateOnly.FromDateTime(DateTime.Now);
        strategySettings.CustomStartTime = DateTime.Now.TimeOfDay.Add(TimeSpan.FromSeconds(-15));
        strategySettings.FastForwardToPresent = false;

        settings.IterationStrategy = new TimeStrategy_Interval(TimeSpan.FromSeconds(10), strategySettings);

        Console.WriteLine("Changed Interation Settings");
        Console.WriteLine("  MaxIterations = 2");
        Console.WriteLine("  IterationStrategy = Interval (Every 10 seconds)");
        Console.WriteLine("Changed Strategy Settings");
        Console.WriteLine($"  CustomStartDate = {strategySettings.CustomStartDate}");
        Console.WriteLine($"  CustomStartTime = {strategySettings.CustomStartTime}");
        Console.WriteLine("  FastForwardToPresent = false");

        createTask = await _taskManager.StartTask(new SimpleShortTask(), TaskExecutionMode.Syncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        settings = new ManagedTaskSettings();
        strategySettings = new TimeStrategySettings();

        Console.WriteLine("\n==Test 10: Synchronous (Custom Settings)==");
        Console.WriteLine("NOTE: Look at output logs");
        settings.MaxIterations = 3;
        settings.AllowParallelIterationExecution = true;
        settings.MaxConcurrentParallelTasks = 2;

        Console.WriteLine("Changed Interation Settings");
        Console.WriteLine("  MaxIterations = 3");
        Console.WriteLine("  AllowParallelIterationExecution = true");
        Console.WriteLine("  MaxConcurrentParallelTasks = 2");

        createTask = await _taskManager.StartTask(new SimpleLongTask(), TaskExecutionMode.Syncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        settings = new ManagedTaskSettings();

        Console.WriteLine("\n==Test 11: Synchronous (Custom Settings)==");
        Console.WriteLine("NOTE: Look at output logs");
        settings.MaxIterations = 2;

        Console.WriteLine("Changed Interation Settings");
        Console.WriteLine("  MaxIterations = 2");

        createTask = await _taskManager.StartTask(new SimpleShortTask(), TaskExecutionMode.Syncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        await Task.Delay(500);

        var tryGetHistory = _taskRegistry.TryGet(createTask.Result.TaskKey);
        if (!tryGetHistory.MethodSuccess)
        {
            throw tryGetHistory.Exception;
        }

        Console.WriteLine("Task Snapshot in Registry");
        string history = tryGetHistory.Result?.GetSnapshotInfo(true)!;
        Console.WriteLine(history);

        settings = new ManagedTaskSettings();
    }
    private async Task TestTaskManagement_AsyncManagedTask()
    {
        Console.WriteLine("----|----|Task Management|----|----");

        var _taskManager = Service_TaskMgmt.TaskManager;
        var _taskRegistry = Service_TaskMgmt.TaskRegistry;
        _taskManager.LogRuntimeSettings();

        OperationResult<IManagedTaskHandle> createTask;
        var settings = new ManagedTaskSettings();
        var strategySettings = new TimeStrategySettings();

        Console.WriteLine("\n==Test 1: Asynchronous (Default Settings)==");
        createTask = await _taskManager.StartTask(new SimpleShortTask(), TaskExecutionMode.Asyncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        Console.WriteLine("Finished creating task. Now waiting until its done.");

        await createTask.Result.RunningTask!;

        settings = new ManagedTaskSettings();

        Console.WriteLine("\n==Test 2: Asynchronous (Custom Settings)==");
        Console.WriteLine("NOTE: Look at output logs");
        settings.MaxIterations = 2;

        Console.WriteLine("Changed Interation Settings");
        Console.WriteLine("  MaxIterations = 2");

        createTask = await _taskManager.StartTask(new SimpleShortTask(), TaskExecutionMode.Asyncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        Console.WriteLine("Finished creating task. Now waiting until its done.");

        await createTask.Result.RunningTask!;

        settings = new ManagedTaskSettings();

        Console.WriteLine("\n==Test 3: Asynchronous (Custom Settings)==");
        Console.WriteLine("NOTE: Look at output logs");
        settings.MaxIterations = 2;
        settings.StopIteratingOnException = false;

        Console.WriteLine("Changed Interation Settings");
        Console.WriteLine("  MaxIterations = 2");
        Console.WriteLine("  StopIteratingOnException = false");

        createTask = await _taskManager.StartTask(new SimpleBrokenTask(), TaskExecutionMode.Asyncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        Console.WriteLine("Finished creating task. Now waiting until its done.");

        await createTask.Result.RunningTask!;

        settings = new ManagedTaskSettings();

        Console.WriteLine("\n==Test 4: Asynchronous (Custom Settings)==");
        Console.WriteLine("NOTE: Look at output logs");
        settings.RetryOnException = true;
        settings.MaxRetryCount = 3;

        Console.WriteLine("Changed Interation Settings");
        Console.WriteLine("  RetryOnException = true");
        Console.WriteLine("  MaxRetryCount = 3");

        createTask = await _taskManager.StartTask(new SimpleBrokenTask(), TaskExecutionMode.Asyncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        Console.WriteLine("Finished creating task. Now waiting until its done.");

        await createTask.Result.RunningTask!;

        settings = new ManagedTaskSettings();

        Console.WriteLine("\n==Test 5: Asynchronous (Custom Settings)==");
        Console.WriteLine("NOTE: Look at output logs");
        settings.MaxIterations = 2;
        settings.RetryOnException = true;
        settings.MaxRetryCount = 3;
        settings.StopIterationAfterMaxRetries = false;

        Console.WriteLine("Changed Interation Settings");
        Console.WriteLine("  MaxIterations = 2");
        Console.WriteLine("  RetryOnException = true");
        Console.WriteLine("  MaxRetryCount = 3");
        Console.WriteLine("  StopIterationAfterMaxRetries = false");

        createTask = await _taskManager.StartTask(new SimpleBrokenTask(), TaskExecutionMode.Asyncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        Console.WriteLine("Finished creating task. Now waiting until its done.");

        await createTask.Result.RunningTask!;

        settings = new ManagedTaskSettings();

        Console.WriteLine("\n==Test 6: Asynchronous (Custom Settings)==");
        Console.WriteLine("NOTE: Look at output logs");
        settings.MaxIterations = 2;
        settings.IterationStrategy = new TimeStrategy_Interval(TimeSpan.FromSeconds(10), strategySettings);

        Console.WriteLine("Changed Interation Settings");
        Console.WriteLine("  MaxIterations = 2");
        Console.WriteLine("  IterationStrategy = Interval (Every 10 seconds)");

        createTask = await _taskManager.StartTask(new SimpleShortTask(), TaskExecutionMode.Asyncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        Console.WriteLine("Finished creating task. Now waiting until its done.");

        await createTask.Result.RunningTask!;

        settings = new ManagedTaskSettings();
        strategySettings = new TimeStrategySettings();

        Console.WriteLine("\n==Test 7: Asynchronous (Custom Settings)==");
        Console.WriteLine("NOTE: Look at output logs");
        settings.MaxIterations = 2;

        strategySettings.SkipFirstIterationWait = false;

        settings.IterationStrategy = new TimeStrategy_Interval(TimeSpan.FromSeconds(10), strategySettings);

        Console.WriteLine("Changed Interation Settings");
        Console.WriteLine("  MaxIterations = 2");
        Console.WriteLine("  IterationStrategy = Interval (Every 10 seconds)");
        Console.WriteLine("Changed Strategy Settings");
        Console.WriteLine("  SkipFirstIterationWait = false");

        createTask = await _taskManager.StartTask(new SimpleShortTask(), TaskExecutionMode.Asyncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        Console.WriteLine("Finished creating task. Now waiting until its done.");

        await createTask.Result.RunningTask!;

        settings = new ManagedTaskSettings();
        strategySettings = new TimeStrategySettings();

        Console.WriteLine("\n==Test 8: Asynchronous (Custom Settings)==");
        Console.WriteLine("NOTE: Look at output logs");
        settings.MaxIterations = 2;

        strategySettings.CustomStartDate = DateOnly.FromDateTime(DateTime.Now);
        strategySettings.CustomStartTime = DateTime.Now.TimeOfDay.Add(TimeSpan.FromSeconds(-15));

        settings.IterationStrategy = new TimeStrategy_Interval(TimeSpan.FromSeconds(10), strategySettings);

        Console.WriteLine("Changed Interation Settings");
        Console.WriteLine("  MaxIterations = 2");
        Console.WriteLine("  IterationStrategy = Interval (Every 10 seconds)");
        Console.WriteLine("Changed Strategy Settings");
        Console.WriteLine($"  CustomStartDate = {strategySettings.CustomStartDate}");
        Console.WriteLine($"  CustomStartTime = {strategySettings.CustomStartTime}");
        Console.WriteLine("NOTE: This will look like its not abiding the custom start date/time but it is. Its just catching up to the current time");

        createTask = await _taskManager.StartTask(new SimpleShortTask(), TaskExecutionMode.Asyncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        Console.WriteLine("Finished creating task. Now waiting until its done.");

        await createTask.Result.RunningTask!;

        settings = new ManagedTaskSettings();
        strategySettings = new TimeStrategySettings();

        Console.WriteLine("\n==Test 9: Synchronous (Custom Settings)==");
        Console.WriteLine("NOTE: Look at output logs");
        settings.MaxIterations = 2;

        strategySettings.CustomStartDate = DateOnly.FromDateTime(DateTime.Now);
        strategySettings.CustomStartTime = DateTime.Now.TimeOfDay.Add(TimeSpan.FromSeconds(-15));
        strategySettings.FastForwardToPresent = false;

        settings.IterationStrategy = new TimeStrategy_Interval(TimeSpan.FromSeconds(10), strategySettings);

        Console.WriteLine("Changed Interation Settings");
        Console.WriteLine("  MaxIterations = 2");
        Console.WriteLine("  IterationStrategy = Interval (Every 10 seconds)");
        Console.WriteLine("Changed Strategy Settings");
        Console.WriteLine($"  CustomStartDate = {strategySettings.CustomStartDate}");
        Console.WriteLine($"  CustomStartTime = {strategySettings.CustomStartTime}");
        Console.WriteLine("  FastForwardToPresent = false");

        createTask = await _taskManager.StartTask(new SimpleShortTask(), TaskExecutionMode.Asyncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        Console.WriteLine("Finished creating task. Now waiting until its done.");

        await createTask.Result.RunningTask!;

        settings = new ManagedTaskSettings();
        strategySettings = new TimeStrategySettings();

        Console.WriteLine("\n==Test 10: Asynchronous (Custom Settings)==");
        Console.WriteLine("NOTE: Look at output logs");
        settings.MaxIterations = 3;
        settings.AllowParallelIterationExecution = true;
        settings.MaxConcurrentParallelTasks = 2;

        Console.WriteLine("Changed Interation Settings");
        Console.WriteLine("  MaxIterations = 3");
        Console.WriteLine("  AllowParallelIterationExecution = true");
        Console.WriteLine("  MaxConcurrentParallelTasks = 2");

        createTask = await _taskManager.StartTask(new SimpleLongTask(), TaskExecutionMode.Asyncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        Console.WriteLine("Finished creating task. Now waiting until its done.");

        await createTask.Result.RunningTask!;

        settings = new ManagedTaskSettings();

        Console.WriteLine("\n==Test 11: Asynchronous (Custom Settings)==");
        Console.WriteLine("NOTE: Look at output logs");
        settings.MaxIterations = 2;

        Console.WriteLine("Changed Interation Settings");
        Console.WriteLine("  MaxIterations = 2");

        createTask = await _taskManager.StartTask(new SimpleShortTask(), TaskExecutionMode.Asyncronous, settings);
        if (!createTask.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask.Exception.Message);
        }

        Console.WriteLine("Finished creating task. Now waiting until its done.");

        while (!createTask.Result.RunningTask!.IsCompleted)
        {
            var tryGet1 = _taskRegistry.TryGet(createTask.Result.TaskKey);
            if (!tryGet1.MethodSuccess)
            {
                throw tryGet1.Exception;
            }

            Debug.WriteLine(tryGet1.Result?.GetSnapshotInfo(true));

            await Task.Delay(2000);
        }

        var tryGetHistory = _taskRegistry.TryGet(createTask.Result.TaskKey);
        if (!tryGetHistory.MethodSuccess)
        {
            throw tryGetHistory.Exception;
        }

        Console.WriteLine("Task Snapshot in Registry");
        string history = tryGetHistory.Result?.GetSnapshotInfo(true)!;
        Console.WriteLine(history);

        settings = new ManagedTaskSettings();
    }



    private async Task TestCoreClasses()
    {
        OperationResult<string> json;

        //json = await FileUtils.ReadFileTextAsync("C:\\NAS\\Home Drive\\Projects\\Junk\\Json1.txt");
        //if (!json.MethodSuccess)
        //{
        //    throw json.Exception;
        //}

        //json = await FileUtils.ReadFileTextAsync("C:\\NAS\\Home Drive\\Projects\\Junk\\Json2.txt");
        //if (!json.MethodSuccess)
        //{
        //    throw json.Exception;
        //}

        json = await FileUtils.ReadFileTextAsync("C:\\NAS\\Home Drive\\Projects\\Junk\\Json3.txt");
        if (!json.MethodSuccess)
        {
            throw json.Exception;
        }

        object result = null;


        //----Get Dictionary by single path----
        //result = JsonUtils.GetDictionary(json.Result).Result; //Root
        //result = JsonUtils.GetDictionary(json.Result, "numbers").Result; //Primitive
        //result = JsonUtils.GetDictionary(json.Result, "data").Result; //Dictionary
        //result = JsonUtils.GetDictionary(json.Result, "house").Result; //Dictionary List
        //result = JsonUtils.GetDictionary(json.Result, "index").Result; //List
        //result = JsonUtils.GetDictionary(json.Result, "data.dictionary1").Result; //Sub Dictionary
        //result = JsonUtils.GetDictionary(json.Result, "data.dictionary1.name").Result; //Sub Dictionary Object
        //result = JsonUtils.GetDictionary(json.Result, "data.activities").Result; //Sub Dictionary List
        //result = JsonUtils.GetDictionary(json.Result, "data.activities.0").Result; //Sub Dictionary List Item
        //result = JsonUtils.GetDictionary(json.Result, "data.activities.0.id").Result; //Sub Dictionary List Item Object

        //----Get List by single path----
        //result = JsonUtils.GetList<int>(json.Result).Result; //Root
        //result = JsonUtils.GetList<int>(json.Result, "testData.luckyNumbers").Result;
        //result = JsonUtils.GetList<double>(json.Result, "testData.prices").Result;
        //result = JsonUtils.GetList<string>(json.Result, "testData.allowedRoles").Result;
        //result = JsonUtils.GetList<int>(json.Result, "testData.matrix.1.2").Result;
        //result = JsonUtils.GetList<int>(json.Result, "testData.emptyList.1").Result;

        //----Get Dictionary List by single path----
        //result = JsonUtils.GetDictionaryList(json.Result).Result; //Root
        //result = JsonUtils.GetDictionaryList(json.Result, "numbers").Result; //Primitive
        //result = JsonUtils.GetDictionaryList(json.Result, "data").Result; //Dictionary
        //result = JsonUtils.GetDictionaryList(json.Result, "house").Result; //Dictionary List
        //result = JsonUtils.GetDictionaryList(json.Result, "index").Result; //List --Should return empty since the objects are not of Dictionary List so we cant convert
        //result = JsonUtils.GetDictionaryList(json.Result, "data.dictionary1").Result; //Sub Dictionary
        //result = JsonUtils.GetDictionaryList(json.Result, "data.dictionary1.name").Result; //Sub Dictionary Object
        //result = JsonUtils.GetDictionaryList(json.Result, "data.activities").Result; //Dictionary List
        //result = JsonUtils.GetDictionaryList(json.Result, "data.activities.0").Result; //Dictionary List Item
        //result = JsonUtils.GetDictionaryList(json.Result, "data.activities.0.id").Result; //Dictionary List Item Object

        //----Get Dictionary by multi-paths----
        //result = JsonUtils.GetDictionary(json.Result, new List<string> { "numbers", "world" }).Result; //Primitives
        //result = JsonUtils.GetDictionary(json.Result, new List<string> { "data", "house" }).Result; //Dictionaries
        //result = JsonUtils.GetDictionary(json.Result, new List<string> { "index", "house" }).Result; //Lists
        //result = JsonUtils.GetDictionary(json.Result, new List<string> { "data.dictionary1", "house.0" }).Result; //Sub Dictionary
        //result = JsonUtils.GetDictionary(json.Result, new List<string> { "data.dictionary1.name", "data.jobs.0" }).Result; //Sub Dictionary Object
        //result = JsonUtils.GetDictionary(json.Result, new List<string> { "data.activities", "data.jobs" }).Result; //Sub Dictionary List
        //result = JsonUtils.GetDictionary(json.Result, new List<string> { "data.activities.0", "data.jobs.0" }).Result; //Sub Dictionary List Item
        //result = JsonUtils.GetDictionary(json.Result, new List<string> { "data.activities.0.id", "data.jobs.0.id" }).Result; //Sub Dictionary List Item Object
        //result = JsonUtils.GetDictionary(json.Result, new List<string> { "data.dictionary1.id", "data.dictionary1.name" }).Result; //Sub Dictionary Same Parent
        //result = JsonUtils.GetDictionary(json.Result, new List<string> { "data.activities.id", "data.activities.name" }).Result; //Sub Dictionary List Same Parent


        //result = JsonUtils.GetDictionary(json.Result, "data.dictionary1", JsonExtractionLayout.PreserveRoot).Result; //Sub Dictionary
        //result = JsonUtils.GetDictionary(json.Result, new List<string> { "data.activities.0.id", "data.jobs.0.id" }).Result; //Sub Dictionary List Item Object
        
        result = JsonUtils.GetDictionary(json.Result, new List<string> { "data.empty", "data.dictionary1" }).Result; //Dictionary List
        //result = JsonUtils.GetDictionaryList(json.Result, "data.empty").Result; //Dictionary List
        //result = JsonUtils.GetList<string>(json.Result, "data.empty").Result; //Dictionary List

        //result = JsonUtils.ParseAndFilterJson<int>(json.Result, new List<string> { "data.empty", "data.dictionary1" }).Result; //Dictionary List

        //result = JsonUtils.GetDictionaryList(json.Result, new List<string> { "data.activities.id", "data.activities.name" }).Result; //Sub Dictionary List Same Parent

        string jsonResult = JsonUtils.SerializeObjectToJson(result).Result;

        Console.WriteLine(jsonResult);
    }

}