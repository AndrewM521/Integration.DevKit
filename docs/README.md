# Integration DevKit
A modular, high-performance .NET Core library packed with general-use utilities for everyday challenges. Designed for flexibility, this devkit allows you to use modules independently or together to streamline your development workflow.

## Architecture: Module / Contract System
To keep your applications lightweight, this DevKit implements an Module/Contract separation pattern.

- Contracts: Contains interfaces, DTOs, and lightweight models. Import this if you only need to define dependencies or implement custom providers.

- Module: Contains the actual concrete implementation and logic. Import this only where you need the heavy lifting done.

This decoupled design minimizes dependencies and prevents assembly bloat in microservices or client applications.

# Module Features
Each module can be used completely independently. Below is a breakdown of what each module provides:

Module | Explanation
------ | -----------
Core | Common extension methods, shared utilities, and object helper utilities
CredentialMgmt | Runtime encryption/decryption of connection secrets. </br> Integrates with: </br> - Files 
CustomLogger | Custom ILogger instances managed concurrently by category name. Automatically routes filtered, formatted logs to a buffered storage registry, debug output, or the console.
ProcessLauncher | Spawns and manages OS-level subprocesses in a non-blocking background monitoring loop with built-in timeout handling. Tracks execution via a concurrent key-mapped manager and captures real-time asynchronous stream data (STDOUT/STDERR).
RESTApiMgmt | Orchestrates HTTP requests with automatic rate-limiting, custom media-type formatting, and request performance metrics. Integrates with: CredentialMgmt for secure endpoint credentials, and manages concurrent clients by name.


SQLMgmt
Provides asynchronous and synchronous execution wrappers for commands and data readers. Integrates with: CredentialMgmt for secure connection string storage, and manages concurrent clients by name.



SQLManager
Thread-Safe Client Management: A collection of SQLClient instances using a ConcurrentDictionary, automatically provisioning defaults if an unconfigured client is requested.
Asynchronous Resource Cleanup: Iterates over all active SQLClients and triggers their individual Dispose() routines.
Global Settings Diagnostics: Scans and logs all public instance configuration properties of the manager, cascade-logging individual client configurations when nested client dictionaries are encountered.

SQLClient
Secret-Store Secured Credentials: Encrypts/Decrypts connection strings, allowing secret store items to be priority over local appsetting values.

Encapsulated Data Reader: Wraps SqlDataReader logic into a managed, asynchronous callback framework so consumer methods can read results without needing to manage data stream lifecycles.
Command Interception Framework: Provides customizable callback hooks (Func<SqlCommand, Task>) allowing developers to programmatically configure parameter collections, query structures, or precise command timeouts safely before transmission.


Garbage Collection Optimization: Calls an explicit post-execution memory optimization macro (GCManager.CallGC_Collect) upon the completion of commands to aggressively free transient buffers and high-volume text payloads.



TaskMgmt
Orchestrates the synchronous or parallel execution of developer-defined ManagedTask abstractions. Controls task lifecycles with strict concurrency limiters (SemaphoreSlim), hooks into host lifetime events for graceful shutdown, and handles complex multi-iteration execution policies—including custom timeout watchdogs, automated exception retries, and sequential or overlapping/parallel iteration strategies.
Task Manager
Task Lifecycle Initialization: Instantiates and validates tasks, configures execution runtimes, and triggers their continuous execution loop synchronously or asynchronously based on configuration.
Task Execution Loop: Regulates continuous task loops, enforces global concurrency caps using a semaphore, and handles sequential versus parallel iteration behavior.
Task Iteration Processing: Handles individual execution cycles of a task by implementing watchdog timeouts, error trapping, tracking states, and managing configured retry or early-termination policies.
Targeted Task Cancellation: Signals an individual running task to cancel using its specific string key.
Global Task Cancellation: Iterates through all currently tracked tasks to trigger a cancellation across the entire system.
Active Task Auditing: Exposes a collection of all string keys associated with tasks that are currently active in the manager.
Batch Task Awaiting: Exposes a utility method to asynchronously wait for an external list of tasks to finish executing.
Configuration Diagnostics: Uses reflection to iterate through and dynamically log all public instance properties of the active runtime settings for debugging purposes.
Graceful Shutdown: Automatically registers a callback with IHostApplicationLifetime to cancel and clean up all active managed tasks when the application host stops.
Task Status Verification: Queries the active task collection to determine if a specific task key is currently in a running or starting state.

ManagedTask
Identity Generation and Enforcement: Requires a unique friendly name upon construction and pairs it with a unique Guid to establish a distinct lookup identity across the system.
Abstract Work Definition: Exposes an abstract method template that subclasses must implement to execute custom asynchronous logic during a task cycle.
Resource Cleanup Lifecycle: Implements the standard IDisposable pattern so derived task instances can safely release unmanaged resources like files, locks, or tokens upon completion.

IManagedTaskIterationHandle
Parent Task Correlation: References the parent task handle to maintain context regarding which higher-level routine owns the running iteration.
Iteration Sequence Tracking: Exposes a unique numerical counter to identify the exact execution cycle number for the current task session.
Start Telemetry Capture: Holds the exact UTC timestamp marking when the specific iteration cycle transitioned into a running state.
Linked Token Cancellation: Exposes a unified cancellation token that fires if either the parent task is globally aborted or this specific iteration is canceled.
Live Duration Tracking: Computes the active elapsed duration of the current execution cycle dynamically in real time.
Execution Status Evaluation: Provides a simple flag to verify if the managed thread loop is actively processing this specific iteration.
Targeted Cycle Cancellation: Triggers a direct abort of the active execution cycle without interrupting the overarching parent task routine.

Thread Locks
Provides a centralized manager to coordinate named synchronous and asynchronous execution bottlenecks. Employs Monitor and SemaphoreSlim alongside reference tracking for automatic memory cleanup.
Lock Acquisition (Async/Sync): Requests a thread lock for a given string key.
Lock Release (Async/Sync): Exits a thread lock for a given string key.

ThreadSafeItems
A suite of extension methods and helper utilities that leverage ThreadLocks. Currently features thread-safe file I/O operations (Read/Write) by resolving distinct file paths as distinct transaction lock keys.
ThreadSafeFileIO
String Writing (Async/Sync): Writes or appends a single text string to a specified file enforcing thread locks.
Line Writing (Async/Sync): Writes or appends an array of strings to a file enforcing thread locks.
Line Reading (Async/Sync): Reads all lines of a file into a string array using a thread lock before accessing the file.
Text Reading (Async/Sync): Reads all content of a file as a single string using a thread lock before accessing the file.







# Quick Start: Adding to Your Project
To integrate the DevKit into your solution, install the required packages via NuGet. You can install each modules engine or just the contracts depending on your project architecture.

## Configuration & Dependency Injection
The DevKit relies on Dependency Injection (DI) and appsettings.json configurations to initialize its modules. You don't need to manually pass connection strings or configuration objects in your code; the engines map directly to your configuration sections.

1. Appsettings.json
Add the configuration block required by the modules you are using:

JSON
{
  "Integration.DevKit": {
    "ModuleName": {
      "ModuleOptions"
    }
  }
}

2. Register Services via DI
Inject the modules into your IServiceCollection. The moduels read from the appsettings:

C#
var host = Host.CreateDefaultBuilder().Build(); //Any object with IServiceProvider is viable here

host.Services.AddCustomLogging(config);

Service_CustomLogger.Initialize(host.Services);

```