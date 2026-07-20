# REST API Management Guide

## Overview

The REST API management package provides a client abstraction for sending HTTP requests, tracking request metrics, and handling API operation results.

## Installation

```bash
dotnet add reference src/Integration.DevKit.RESTApiMgmt/Integration.DevKit.RESTApiMgmt.csproj
```

## Requirements

- .NET 8
- HttpClient support
- A configured service provider with REST API services registered

## Quick Start

```csharp
using Integration.DevKit.RESTApiMgmt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddRESTApiMgmt(new ConfigurationBuilder().AddInMemoryCollection().Build());
    });

var app = builder.Build();
Service_RESTApiMgmt.Initialize(app.Services);

var client = Service_RESTApiMgmt.ApiManager.GetClient("my-client");
var result = await client.GetAsync("https://example.com/api/ping");
```

## Configuration

Configure the API client and manager through the available settings objects and configuration infrastructure.

## Core Usage

```csharp
var result = await client.GetAsync("https://example.com/api/items");
if (result.MethodSuccess)
{
    Console.WriteLine(result.Result);
}
else
{
    Console.WriteLine(result.Exception.Message);
}
```

## API Reference

### ApiClient

- Purpose: executes requests against a configured HTTP endpoint
- Methods: `GetAsync`, `PutAsync`, `PostAsync`, `DeleteAsync`

### ApiManager

- Purpose: manages one or more API clients and their settings.

## Data Models

- ApiOperationResult<T>
- ApiClientSettings
- ApiManagerSettings

## Error Handling

Failures are captured in `ApiOperationResult<T>` and exposed through the `MethodSuccess` and `Exception` members.

## Best Practices

- Use named clients or explicit settings for local development and production.
- Validate base URLs and timeouts.
- Handle both HTTP failures and deserialization errors explicitly.
