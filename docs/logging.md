# Custom Logging Guide

## Overview

The custom logging package provides a logger implementation that can write to the console, debug output, and a file-backed buffer. It integrates with the Microsoft.Extensions.Logging abstractions.

## Installation

```bash
dotnet add reference src/Integration.DevKit.CustomLogger/Integration.DevKit.CustomLogger.csproj
dotnet add reference src/Integration.DevKit.CustomLogger.Flusher/Integration.DevKit.CustomLogger.Flusher.csproj
```

## Requirements

- .NET 8
- Microsoft.Extensions.Logging.Abstractions
- An initialized service provider

## Quick Start

```csharp
using Integration.DevKit.CustomLogger;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddCustomLogging(new ConfigurationBuilder().AddInMemoryCollection().Build());
    });

var app = builder.Build();
Service_CustomLogger.Initialize(app.Services);

var logger = Service_CustomLogger.LoggerManager.GetLogger("MyApp");
logger.LogInformation("Hello from Integration.DevKit");
```

## Configuration

The logger manager and flusher use runtime settings to determine console output, file output, and minimum log levels.

## Core Usage

```csharp
var logger = Service_CustomLogger.LoggerManager.GetLogger("MyApp");
logger.EnableConsoleOutput();
logger.LogWarning("This will be written to the console");
logger.DisableConsoleOutput();
```

## API Reference

### CustomLogger

- Purpose: implements `ILogger` and writes messages to configured outputs
- Methods: `BeginScope<TState>`, `IsEnabled`, `Log<TState>`, `EnableLogger`, `DisableLogger`, `EnableConsoleOutput`, `DisableConsoleOutput`

### CustomLoggerManager

- Purpose: creates and manages logger instances.

### LogFlusher

- Purpose: persists buffered log messages based on runtime settings.

## Error Handling

Logging calls should not normally throw, but initialization and service resolution may fail if dependencies are missing.

## Best Practices

- Initialize logging services before using the logger.
- Keep logging enabled for operational diagnostics.
- Avoid excessive log volume in high-frequency loops.
