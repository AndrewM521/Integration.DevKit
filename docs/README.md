# Integration.DevKit Documentation

Integration.DevKit is a .NET SDK that provides reusable building blocks for configuration protection, logging, REST API access, task management, thread-safe file access, and SQL client management. The library is designed for developers who want a pragmatic set of services and helpers that can be wired into a host application with minimal setup.

## Overview

The SDK is organized into several feature areas:

- Core utilities for configuration protection, JSON helpers, file helpers, and on-demand hosting support
- Custom logging and log flushing services for application diagnostics
- REST API management with typed request and result models
- Task management for recurring or long-running work
- Thread-safe helpers for file I/O and lock coordination
- SQL management helpers for database access and connection testing

## Requirements

- .NET 8 SDK or later
- A .NET application that can reference the Integration.DevKit projects or NuGet packages
- Optional: configuration files such as appsettings.json for service initialization

## Installation

Add the relevant projects to your solution or reference the published NuGet packages.

### Example: referencing the projects

```bash
dotnet add reference src/Integration.DevKit.Core/Integration.DevKit.Core.csproj
dotnet add reference src/Integration.DevKit.CustomLogger/Integration.DevKit.CustomLogger.csproj
dotnet add reference src/Integration.DevKit.RESTApiMgmt/Integration.DevKit.RESTApiMgmt.csproj
dotnet add reference src/Integration.DevKit.TaskMgmt/Integration.DevKit.TaskMgmt.csproj
dotnet add reference src/Integration.DevKit.SQLMgmt/Integration.DevKit.SQLMgmt.csproj
```

## Quick Start

The sample app in the repository demonstrates a working setup. The typical startup flow is:

1. Create configuration protection providers.
2. Build an IConfiguration from your appsettings.json file.
3. Register DevKit services in the host.
4. Initialize the services through the provided service bootstrap classes.
5. Start the host and use the feature services.

### Minimal example

```csharp
using Integration.DevKit.Core.Configuration;
using Integration.DevKit.Core.OnDemand;
using Integration.DevKit.CustomLogger;
using Integration.DevKit.CustomLogger.Flusher;
using Integration.DevKit.RESTApiMgmt;
using Integration.DevKit.TaskMgmt;
using Integration.DevKit.SQLMgmt;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var configuration = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false)
    .Build();

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddCustomLogging(configuration);
        services.AddCustomLogFlusher(configuration);
        services.AddRESTApiMgmt(configuration);
        services.AddTaskMgmt(configuration);
        services.AddSQLMgmt(configuration);
    });

var app = builder.Build();

Service_CustomLogger.Initialize(app.Services);
Service_CustomLogFlusher.Initialize(app.Services);
Service_RESTApiMgmt.Initialize(app.Services);
Service_TaskMgmt.Initialize(app.Services);
Service_SQLMgmt.Initialize(app.Services);

await app.StartAsync();
```

## Configuration

Configuration is usually provided through IConfiguration and appsettings.json. Many services expect the configuration root to contain sections relevant to their feature area.

### Common configuration pattern

```json
{
  "Integration": {
    "DevKit": {
      "Logging": {
        "OutputLogLevel": "Information"
      },
      "RESTApiMgmt": {
        "Default_HttpTimeout_Seconds": 30
      },
      "TaskMgmt": {
        "DefaultMaxIterations": 5
      }
    }
  }
}
```

## Core Usage

### Configuration protection

Configuration values can be encrypted and decrypted using the built-in protectors.

```csharp
using Integration.DevKit.Core.Configuration;

var protector = new AesConfigProtector("my-super-secret-32-byte-long-key!!", "1234567890123456");
var encrypted = protector.Encrypt("my-value");
var decrypted = protector.Decrypt(encrypted);
```

### Logging

The custom logger can be resolved from the service provider after initialization.

```csharp
using Integration.DevKit.CustomLogger;

var logger = Service_CustomLogger.LoggerManager.GetLogger("MyApp");
logger.LogInformation("Application started");
```

## API Reference

The following sections document the main public components in the SDK.

- Core: configuration protection, file helpers, JSON helpers, result models, on-demand hosting
- Logging: logger, manager, logger registry, log flusher
- REST API: API client, API manager, API request helpers, request/response models
- Task management: managed task handles, task manager, strategies, settings
- Thread locks: lock manager and lock state types
- SQL management: SQL client and manager interfaces

## Data Models

Key data models include:

- OperationResult<T>
- NullableOperationResult<T>
- ApiOperationResult<T>
- ConfigProtectorContract
- EncryptionOptions
- ApiClientSettings
- ManagedTaskSettings
- TimeStrategySettings

## Error Handling

Most SDK operations return result objects rather than throwing directly. This pattern lets callers inspect method success and retrieve exceptions without relying on try/catch for simple flows.

```csharp
var result = await someOperation();
if (!result.MethodSuccess)
{
    Console.WriteLine(result.Exception.Message);
}
```

## Best Practices

- Prefer the typed result objects when a call may fail or return a nullable payload.
- Initialize services before using them.
- Keep configuration secrets out of source control.
- Use the logger infrastructure consistently for diagnostics rather than Console.WriteLine in production paths.
- Validate settings before creating clients or tasks.

## Troubleshooting

### Services are not initialized

Ensure that the relevant service initialization method is called after building the host.

### Configuration values are not decrypted

Verify that the same protector configuration and signature are used when encrypting and decrypting.

### Logging output is missing

Check the logger manager runtime settings and the log flush configuration.

## FAQ

### Is this SDK production-ready?

The SDK provides reusable abstractions and service helpers, but each application should validate its own configuration and runtime requirements.

### Can I use only one feature area?

Yes. Each feature area is implemented as a separate namespace and service layer, so you can adopt only what you need.
