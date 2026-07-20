# Task Management Guide

## Overview

The task management package provides recurring or long-running task execution with strategies, snapshots, and lifecycle handles.

## Installation

```bash
dotnet add reference src/Integration.DevKit.TaskMgmt/Integration.DevKit.TaskMgmt.csproj
```

## Requirements

- .NET 8
- A configured service provider with task management services registered

## Quick Start

```csharp
using Integration.DevKit.TaskMgmt;
using Integration.DevKit.TaskMgmt.Contracts;

var settings = new ManagedTaskSettings
{
    MaxIterations = 1
};

var task = new ManagedTask("sample-task", new SimpleTask());
```

## Configuration

Task behavior is configured through `ManagedTaskSettings`, `TaskManagerSettings`, and strategy objects such as `TimeStrategy_Interval`.

## Core Usage

```csharp
var strategy = new TimeStrategy_Interval(TimeSpan.FromMinutes(5), new TimeStrategySettings());
var taskSettings = new ManagedTaskSettings
{
    IterationStrategy = strategy,
    MaxIterations = -1
};
```

## API Reference

### TaskManager

- Purpose: starts and tracks managed tasks
- Main method: `StartTask`

### ManagedTaskHandle

- Purpose: exposes runtime state and control for an executing task

### ManagedTaskIterationHandle

- Purpose: exposes state and cancellation for a task iteration

## Data Models

- ManagedTaskSettings
- ManagedTaskState
- TimeStrategySettings
- ManagedTaskSnapshot

## Error Handling

Task operations return `OperationResult<T>`-style results, so callers can inspect success state and exceptions before acting on the result.

## Best Practices

- Use explicit iteration strategies for recurring work.
- Review task state transitions before restarting a task.
- Make task body logic idempotent when possible.
