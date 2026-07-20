# SQL and Locking Guide

## Overview

This portion of Integration.DevKit provides helpers for SQL client management and thread-safe synchronization primitives.

## Installation

```bash
dotnet add reference src/Integration.DevKit.SQLMgmt/Integration.DevKit.SQLMgmt.csproj
dotnet add reference src/Integration.DevKit.ThreadLocks/Integration.DevKit.ThreadLocks.csproj
dotnet add reference src/Integration.DevKit.ThreadSafeItems/Integration.DevKit.ThreadSafeItems.csproj
```

## Requirements

- .NET 8
- A configured service provider
- Access to the target SQL server when using SQL management helpers

## Quick Start

```csharp
using Integration.DevKit.SQLMgmt;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var builder = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        services.AddSQLMgmt(new ConfigurationBuilder().AddInMemoryCollection().Build());
    });
```

## Core Usage

### SQL client

```csharp
var sqlClient = Service_SQLMgmt.SQLManager.GetClient("my-client");
var result = await sqlClient.TestSqlConnectionAsync();
```

### Thread locks

```csharp
var lockManager = Service_ThreadLocks.ThreadLockManager;
```

## API Reference

### SQLClient

- Purpose: manages SQL connection settings and executes connection tests
- Main method: `TestSqlConnectionAsync`

### ThreadLockManager

- Purpose: provides named mutual-exclusion primitives for thread coordination.

## Error Handling

SQL operations report failures through result objects, while lock operations should be used with appropriate timeout handling.

## Best Practices

- Keep connection strings in protected configuration stores.
- Use lock primitives sparingly and only for shared critical paths.
- Validate connection information before running database operations.
