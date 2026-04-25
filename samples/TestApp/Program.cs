using AndrewM5.DevKit.ApiClientManagement;
using AndrewM5.DevKit.ApiClientManagement.Contracts.Interfaces;
using AndrewM5.DevKit.ApiClientManagement.Contracts.Models;
using AndrewM5.DevKit.ApiClientManagement.Services;
using AndrewM5.DevKit.Core;
using AndrewM5.DevKit.CredentialManagement.Services;
using AndrewM5.DevKit.CustomLogger.Flusher.Services;
using AndrewM5.DevKit.Logging.Services;
using AndrewM5.DevKit.ProcessLauncher;
using AndrewM5.DevKit.ProcessLauncher.Services;
using AndrewM5.DevKit.TaskManagement;
using AndrewM5.DevKit.TaskManagement.Contracts.Interfaces;
using AndrewM5.DevKit.TaskManagement.Contracts.Models;
using AndrewM5.DevKit.TaskManagement.Services;
using AndrewM5.DevKit.ThreadSafeItems.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace TestApp;

public class Program
{
    public static async Task Main(string[] args)
    {
        var config = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json", false)
            .Build();

        var host = Host.CreateDefaultBuilder(args)
            .ConfigureServices((context, services) =>
            {
                services.AddCustomLogging(config);
                services.AddCustomLogFlusher(config);
                //services.AddProcessLauncher();
                //services.AddTaskManagement(config);
                //services.AddThreadSafeItems();
                //services.AddApiManagement(config);
                //services.AddFileSecretStore("TestApp", "C:\\Users\\andre\\Projects\\Junk\\Secrets", "C:\\Users\\andre\\Projects\\Junk\\Keys");

                // Register your app entry
                services.AddSingleton<AppEntry>();
            })
            .Build();

        LoggingHost.Initialize(host.Services);
        LogFlusherHost.Initalize(host.Services);
        //ProcessLauncherHost.Initialize(host.Services);
        //TaskManagementHost.InitializeTaskManagement(host.Services);
        //ThreadSafeItemsHost.Initialize(host.Services);
        //ApiManagementHost.Initialize(host.Services);
        //CredentialManagementHost.InitializeFileSecretStore(host.Services);

        // Start hosted services (LogFlushService will start running in the background)
        await host.StartAsync();

        // Run your application
        var app = host.Services.GetRequiredService<AppEntry>();

        await app.RunAsync(args);

        await host.StopAsync();
    }
}

public class AppEntry
{

    public async Task RunAsync(string[] args)
    {
        //await TaskCoreClasses(); 

        //await TestLogger();
        await TestCustomLoggerFlusher();
        //await TestProcessLauncher();


        //await TestTaskManagement();
        //await TestTaskScheduling();
        //await TestTaskConcurrency(5000);
        //await TestThreadSafeItems();
        //await TestApiManagement();
        //await TestApiManagementCredentials(true, false);
        //await TestFileSecretStore();

        Console.WriteLine("Press enter to exit");
        Console.ReadLine();
    }

    private void TestCustomLogger()
    {
        var _loggerManager = LoggingHost.LoggerManager;

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
        var _loggerManager = LoggingHost.LoggerManager;
        var _logFlushService = LogFlusherHost.LogFlushService;

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

    private async Task TaskCoreClasses()
    {
        object jsonObject = null;

        jsonObject = new Dictionary<string, object>
        {
            ["name"] = "test",
            ["data"] = new Dictionary<string, object>
            {
                ["type"] = "type1",
                ["count"] = 1,
                ["tags"] = new List<string?> {
                    "tag1",
                    "tag2",
                    null
                },
                ["types"] = new List<Dictionary<string, object>> {
                    new Dictionary<string, object>{ ["name"] = "apple" },
                    new Dictionary<string, object>{ ["name"] = "banana" },
                    new Dictionary<string, object>{ ["name"] = "grape" },
                }
            }
        };

        //jsonObject = new List<string> {
        //    "name",
        //    "tag",
        //    "type"
        //};

        //jsonObject = new List<string> {};

        var getJson = JsonUtils.SerializeObjectToJson(jsonObject);

        //var keys = new List<string> { "name", "data.tags", "name.count" };
        var keys = new List<string> { };

        var getDict = JsonUtils.ParseAndFilterJson<Dictionary<string, object>>(getJson.Result, keys);
        if (!getDict.MethodSuccess)
        {
            throw getDict.Exception;
        }

        int? dictVal;
        Dictionary<string, object>? dictionary;
        List<Dictionary<string, object>>? listDictionary;


        //Valid key
        dictVal = JsonUtils.GetDictionaryValue<int>(getDict.Result, "data.count").Result;
        dictionary = JsonUtils.GetDictionary(getDict.Result, "data").Result;
        listDictionary = JsonUtils.GetListDictionary(getDict.Result, "data.types").Result;

        //Missing Key
        dictVal = JsonUtils.GetDictionaryValue<int>(getDict.Result, "data.count1").Result;
        dictionary = JsonUtils.GetDictionary(getDict.Result, "data1").Result;
        listDictionary = JsonUtils.GetListDictionary(getDict.Result, "data.types1").Result;

        //Type Exception
        dictVal = JsonUtils.GetDictionaryValue<int>(getDict.Result, "name").Result;
        dictionary = JsonUtils.GetDictionary(getDict.Result, "data.tags").Result;
        listDictionary = JsonUtils.GetListDictionary(getDict.Result, "data.tags").Result;
    }
    private async Task TestFileSecretStore()
    {
        var store = CredentialManagementHost.FileSecretStore;

        // Set a key
        var setResult = store.SetKey("Api", "ApiKey", "super-secret-value");
        if (!setResult.MethodSuccess)
        {
            Console.WriteLine($"Set failed: {setResult.Exception}");
            return;
        }

        // Get a key
        var getResult = store.GetKey("Api", "ApiKey");
        if (getResult.MethodSuccess)
        {
            Console.WriteLine($"Retrieved value: {getResult.Result}");
        }
        else
        {
            Console.WriteLine($"Get failed: {getResult.Exception}");
        }

        // Delete the key
        var deleteKeyResult = store.DeleteKey("Api", "ApiKey");
        if (!deleteKeyResult.MethodSuccess)
        {
            Console.WriteLine($"DeleteKey failed: {deleteKeyResult.Exception}");
            return;
        }

        Console.WriteLine("Key deleted successfully.");

        // Verify deletion
        var verifyResult = store.GetKey("Api", "ApiKey");
        if (!verifyResult.MethodSuccess)
        {
            Console.WriteLine("Verified: key no longer exists.");
        }
        else
        {
            Console.WriteLine("DeleteKey failed — key still exists.");
        }

        // Delete secret file
        var deleteResult = store.DeleteSecret("Api");
        if (!deleteResult.MethodSuccess)
        {
            Console.WriteLine($"Delete failed: {deleteResult.Exception}");
        }
    }
    private async Task TestApiManagementCredentials(bool includeSecretStore = false, bool deleteCreds = false)
    {
        var _apiManager = ApiManagementHost.ApiManager;
        var _client = _apiManager.GetClient("TestClient");

        _apiManager.LogRuntimeSettings();

        Console.WriteLine("----------------------------------------------------");
        Console.WriteLine("Credential Test:");

        if (includeSecretStore)
        {
            _client.SetSecretStore(CredentialManagementHost.FileSecretStore);

            // Set credentials
            var setCreds = _client.SetCredentials("testUser", "testPassword");
            if (!setCreds.MethodSuccess)
            {
                Console.WriteLine($"SetCredentials failed: {setCreds.Exception?.Message}");
                return;
            }

            Console.WriteLine("Credentials stored.");
        }

        var getUsername = _client.GetUsername();
        if (!getUsername.MethodSuccess)
        {
            throw getUsername.Exception;
        }

        Console.WriteLine("Username: " + getUsername.Result);

        var getPassword = _client.GetPassword();
        if (!getPassword.MethodSuccess)
        {
            throw getPassword.Exception;
        }

        Console.WriteLine("Password: " + getPassword.Result);

        if (includeSecretStore && deleteCreds)
        {
            // Delete password only (example of DeleteCredentialKey)
            var deletePassword = _client.DeleteCredential("password");
            if (!deletePassword.MethodSuccess)
            {
                Console.WriteLine($"DeleteCredentialKey failed: {deletePassword.Exception?.Message}");
                return;
            }

            Console.WriteLine("Password credential deleted.");

            // Get password
            var passwordResult1 = _client.GetPassword();
            if (!passwordResult1.MethodSuccess)
            {
                Console.WriteLine($"GetPassword failed: {passwordResult1.Exception?.Message}");
            }

            var deleteAll = _client.DeleteAllCredentials();
            if (!deleteAll.MethodSuccess)
            {
                Console.WriteLine($"DeleteCredentials failed: {deleteAll.Exception?.Message}");
                return;
            }

            Console.WriteLine("All credentials deleted.");
        }
    }
    private async Task TestApiManagement()
    {
        ApiEndpoint Posts = new ApiEndpoint("/Posts");

        var _apiManager = ApiManagementHost.ApiManager;
        var _client = _apiManager.GetClient("TestClient");

        _apiManager.LogRuntimeSettings();

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

        var getJsonPOST = JsonUtils.SerializeObjectToJson(new Dictionary<string, object> { 
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

        var buildUrlDELETE = Posts.BuildPositionalUrl(new List<object?> { 1 });
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
    private async Task TestThreadSafeItems()
    {
        Console.WriteLine("----|----|Thread Safe FileIO |----|----");

        var _threadSafeFileIO = ThreadSafeItemsHost.ThreadSafeFileIOClass;
        string filePath = "C:\\Users\\andre\\Projects\\Junk\\threadSafeTest.txt";

        var writeToFile = await _threadSafeFileIO.WriteToFileAsync(filePath, "This is a test message");
        if (!writeToFile.MethodSuccess)
        {
            Console.WriteLine(writeToFile.Exception);
            return;
        }

        var readFile = await _threadSafeFileIO.ReadFileTextAsync(filePath);
        if (!readFile.MethodSuccess)
        {
            Console.WriteLine(readFile.Exception);
            return;
        }

        Console.WriteLine(readFile.Result);
    }
    
    private async Task TestProcessLauncher()
    {
        var _processManager = ProcessLauncherHost.ProcessManager;

        var processConfig = new ManagedProcessConfig
        {
            ProcessKey = "PingTest",
            Command = "cmd.exe",
            Arguments = "/c ping 127.0.0.1 -n 4",
            ShowWindow = true,
            TimeoutSeconds = 10,
            WorkingDirectory = Environment.CurrentDirectory
        };

        var startResult = _processManager.StartProcess(processConfig);
        if (startResult.MethodSuccess)
        {
            Console.WriteLine($"Process {startResult.Result.ProcessKey} started successfully.");
        }
        else
        {
            Console.WriteLine($"Failed to start process: {startResult.Exception}");
        }
    }

    private async Task TestTaskManagement()
    {
        var logger = LoggingHost.LoggerManager.GetLogger("App");
        var _taskManager = TaskManagementHost.TaskManager;
        var _taskRegistry = TaskManagementHost.TaskRegistry;

        _taskManager.LogRuntimeSettings();

        Console.WriteLine("Synchronous Test");
        var settings = new ManagedTaskSettings();
        var strategySettings = new TimeStrategySettings();
        settings.MaxIterations = 3;
        //settings.RetryOnException = true;
        //settings.MaxRetryCount = 2;
        //settings.StopIterationAfterMaxRetries = false;
        settings.StopIteratingOnException = false;

        strategySettings.CustomStartDate = DateOnly.FromDateTime(DateTime.Now);
        //strategySettings.CustomStartTime = new TimeSpan(0,5,0,0);
        strategySettings.FastForwardToPresent = true;
        strategySettings.SkipFirstIterationWait = true;
        //settings.IterationStrategy = new TimeStrategy_Interval(TimeSpan.FromMinutes(1), strategySettings);
        //settings.ContinueIteration = new TimeStrategy_Daily();
        //settings.AllowParallelIterationExecution = true;
        //settings.MaxConcurrentParallelTasks = 2;

        var createTask0 = await _taskManager.StartTask(new SimpleBrokenTask(), TaskExecutionMode.Asyncronous, settings);
        if (!createTask0.MethodSuccess)
        {
            Console.WriteLine("Error: " + createTask0.Exception.Message);
        }

        Console.WriteLine($"End Time: {createTask0.Result?.EndDTM}. Elapsed Time: {createTask0.Result?.Runtime}");

        while (!createTask0.Result.RunningTask.IsCompleted)
        {
            var tryGet1 = _taskRegistry.TryGet(createTask0.Result.TaskKey);
            if (!tryGet1.MethodSuccess)
            {
                throw tryGet1.Exception;
            }

            logger?.LogInformation(tryGet1.Result?.GetSnapshotInfo(true));

            await Task.Delay(1000);
        }
        var tryGet = _taskRegistry.TryGet(createTask0.Result.TaskKey);
        if (!tryGet.MethodSuccess)
        {
            throw tryGet.Exception;
        }

        logger?.LogInformation(tryGet.Result?.GetSnapshotInfo(true));


        //Console.WriteLine("Restart Test");
        //var restartTask = await _taskManager.RestartTask(createTask0.Result.TaskKey!);
        //if (!restartTask.MethodSuccess)
        //{
        //    Console.WriteLine("Error: " + restartTask.Exception.Message);
        //}

        //var getRuntime = createTask0.Result.GetTaskRuntime();
        //if (!getRuntime.MethodSuccess)
        //{
        //    Console.WriteLine("Error: " + getRuntime.Exception.Message);
        //}

        //Console.WriteLine($"Task0 runtime: {getRuntime.Result.TotalSeconds:N2}s");

        //Console.WriteLine("Asynchronous Test");
        //var task1 = new SimpleTestTask();
        //var task2 = new SimpleTestTask();

        //var createTask1 = await _taskManager.StartTask(task1, TaskExecutionMode.Asyncronous);
        //var createTask2 = await _taskManager.StartTask(task2, TaskExecutionMode.Asyncronous);

        //// 6️⃣ Wait for tasks to finish
        //await Task.WhenAll(createTask1.Result.RunningTask!, createTask2.Result.RunningTask!);

        //Console.WriteLine($"Task1 runtime: {createTask1.Result.GetTaskRuntime().Result.TotalSeconds:N2}s");
        //Console.WriteLine($"Task2 runtime: {createTask2.Result.GetTaskRuntime().Result.TotalSeconds:N2}s");

        //DisplayTaskSnapshot(createTask0.Result.TaskKey, logger);
        //DisplayTaskSnapshot(createTask1.Result.TaskKey, logger);
        //DisplayTaskSnapshot(createTask2.Result.TaskKey, logger);

        //Console.WriteLine("All tasks completed.");
    }
}