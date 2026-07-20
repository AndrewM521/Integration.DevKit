# Core SDK Guide

## Overview

The core package provides utility classes for configuration protection, file operations, JSON handling, result types, and on-demand hosting support.

## Installation

Add the core project to your solution:

```bash
dotnet add reference src/Integration.DevKit.Core/Integration.DevKit.Core.csproj
```

## Requirements

- .NET 8
- Microsoft.Extensions.Configuration abstractions
- Microsoft.Extensions.Hosting abstractions

## Quick Start

```csharp
using Integration.DevKit.Core.Configuration;

var protector = new AesConfigProtector("my-super-secret-32-byte-long-key!!", "1234567890123456");
var encrypted = protector.Encrypt("hello");
var decrypted = protector.Decrypt(encrypted);

Console.WriteLine(decrypted);
```

## Configuration Protection

### IConfigProtector

The interface provides the contract for encrypting and decrypting configuration values.

### Implementations

- AesConfigProtector
- Base64ConfigProtector

### Example

```csharp
using Integration.DevKit.Core;
using Integration.DevKit.Core.Configuration;

var contract = new ConfigProtectorContract('|')
{
    Signature = "ENC",
    Version = "v1"
};

var configBuilder = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: false);
```

## Result Types

The core package provides result wrappers that make it easier to work with operations that may fail.

### OperationResult<T>

Use this when the operation should always return a non-null value on success.

### NullableOperationResult<T>

Use this when the operation may legitimately return null.

### NullOperationResult

Use this when the operation does not need a payload and only needs success/failure information.

## File and Directory Helpers

The core package also includes helpers for file I/O and directory management.

## API Reference

### AesConfigProtector

- Purpose: provides AES-based configuration protection
- Constructor: `AesConfigProtector(string encryptionKey, string iv)`
- Method: `Encrypt(string plainText)`
- Method: `Decrypt(string cipherText)`

### Base64ConfigProtector

- Purpose: provides simple base-64 protection for configuration values
- Method: `Encrypt(string plainText)`
- Method: `Decrypt(string cipherText)`

### ConfigProtectorContract

Represents metadata used when identifying protected configuration values.

## Error Handling

Configuration protection methods may throw `ArgumentNullException` when required values are null or empty.

## Best Practices

- Use a secure key and IV source.
- Do not store encryption keys in source control.
- Prefer the typed result models for operations that can fail.
