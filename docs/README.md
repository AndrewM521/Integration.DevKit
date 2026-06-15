# Integration DevKit
A modular, high-performance .NET Core library packed with general-use utilities for everyday coding challenges. Designed for flexibility, this devkit allows you to use modules independently or together to streamline your development workflow.

## Architecture: Engine / Contract System
To keep your applications lightweight, this DevKit implements an Engine/Contract separation pattern.

- Contracts: Contains interfaces, DTOs, and lightweight models. Import this if you only need to define dependencies or implement custom providers.

- Engines: Contains the actual concrete implementation and logic. Import this only where you need the heavy lifting done.

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
SQLMgmt | Provides asynchronous and synchronous execution wrappers for commands and data readers. Integrates with: CredentialMgmt for secure connection string storage, and manages concurrent clients by name.
TaskMgmt | Orchestrates the synchronous or parallel execution of developer-defined ManagedTask abstractions. Controls task lifecycles with strict concurrency limiters (SemaphoreSlim), hooks into host lifetime events for graceful shutdown, and handles complex multi-iteration execution policies—including custom timeout watchdogs, automated exception retries, and sequential or overlapping/parallel iteration strategies.
Thread Locks | Provides a centralized manager to coordinate named synchronous and asynchronous execution bottlenecks. Employs Monitor and SemaphoreSlim alongside reference tracking for automatic memory cleanup.
Thread Safe Items | A suite of extension methods and helper utilities that leverage ThreadLocks. Currently features thread-safe file I/O operations (Read/Write) by resolving distinct file paths as distinct transaction lock keys.

# Quick Start: Adding to Your Project
To integrate the DevKit into your solution, install the required packages via NuGet. You can install each modules engine or just the contracts depending on your project architecture.

## Configuration & Dependency Injection
The DevKit relies heavily on Dependency Injection (DI) and standard appsettings.json configurations to initialize its module engines. You don't need to manually pass connection strings or configuration objects in your code; the engines map directly to your configuration sections.

1. Update appsettings.json
Add the configuration block required by the modules you are using:

JSON
{
  "Integration.DevKit": {
    "SQLMgmt": {
      "ConnectionString": "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;"
    },
    "CustomLogger": {
      "MinimumLevel": "Information",
      "LogToConsole": true
    },
    "RESTApiMgmt": {
      "BaseUrl": "https://api.example.com",
      "TimeoutSeconds": 30
    }
  }
}

2. Register Services via DI
Inject the modules into your IServiceCollection. The engines automatically read from the setup configuration:

C#
using DevKit.SQLMgmt.Contracts;
using DevKit.SQLMgmt.Engine;

public void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    // Bind configurations and register the engine against its contract
    services.AddSqlEngine(configuration.GetSection("DevKit:SQLMgmt"));
    
    // Alternative explicit registration if required:
    // services.AddSingleton<ISqlEngine, SqlEngine>();
}
```