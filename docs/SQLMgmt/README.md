# Integration.DevKit.SQLMgmt

Named SQL Server clients with connection testing and command/data-reader helpers that return result objects instead of throwing.

## What it is

`Integration.DevKit.SQLMgmt` provides named SQL Server client management: you define clients by name in configuration, resolve them through a manager, and run commands or data readers through helpers that handle connection lifetime and return the SDK's shared result-object types. It also includes `SQLUtils`, extension methods for turning a `DbDataReader` into dictionaries or CSV rows.

## Why it exists

Raw ADO.NET means repeating the same boilerplate for every query: open a connection, build a command, add parameters, wrap everything in `try`/`catch`/`using`, and copy reader columns into something usable. Integration apps also tend to talk to more than one database. This module:

- Keeps connection details in configuration, looked up by client name, so code doesn't carry connection strings around.
- Handles connection open/close (or a single serialized connection, if your provider needs it) for you.
- Returns failures as `OperationResult<T>`/`NullOperationResult` so a failed query is a branch, not an exception.
- Optionally reads the connection string from an encrypted [secret store](https://github.com/AndrewM521/AndrewM5.DevKit/blob/main/docs/CredentialMgmt/README.md) instead of plain configuration.

## Requirements & installation

- .NET 8, 9 or 10
- `Microsoft.Data.SqlClient`
- Network access to the target SQL Server
- [Integration.DevKit.Core](https://github.com/AndrewM521/AndrewM5.DevKit/blob/main/docs/Core/README.md) and [Integration.DevKit.CredentialMgmt.Contracts](https://github.com/AndrewM521/AndrewM5.DevKit/blob/main/docs/CredentialMgmt.Contracts/README.md) (pulled in automatically)

```bash
dotnet add package Integration.DevKit.SQLMgmt
# or, from source
dotnet add reference src/Integration.DevKit.SQLMgmt/Integration.DevKit.SQLMgmt.csproj
```

NuGet: [Integration.DevKit.SQLMgmt](https://www.nuget.org/packages/Integration.DevKit.SQLMgmt)

## Getting started

```csharp
using Integration.DevKit.SQLMgmt;

services.AddSQLMgmt(configuration);
// ... build the host ...
Service_SQLMgmt.Initialize(app.Services);

var sqlClient = Service_SQLMgmt.SQLManager.GetClient("my-client");
var pingResult = await sqlClient.TestSqlConnectionAsync();
```

`TestSqlConnectionAsync` doesn't just open and close a connection — it executes `SELECT 1;` against the database, so the configured credentials need at least query permission, not merely the ability to connect.

## Configuration

`AddSQLMgmt` binds the config section `Integration.DevKit:SQLManagement` to `SQLManagerSettings`:

```json
{
  "Integration.DevKit": {
    "SQLManagement": {
      "EnableLogging": true,
      "Clients": {
        "my-client": {
          "ConnectionString": "Server=...;Database=...;",
          "UseSingleConnection": false
        }
      }
    }
  }
}
```

| Property | Default | Notes |
| --- | --- | --- |
| `ConnectionString` | `""` | Ignored if a secret store is attached (see below) — the store takes priority. |
| `UseSingleConnection` | `false` | `false`: a new `SqlConnection` is opened and disposed per call. `true`: one persistent connection is reused and serialized behind an internal semaphore — useful for providers with expensive connection setup, but means all calls on that client queue behind each other. |

`SQLManager.GetClient(clientName)` caches one `SQLClient` per name (case-insensitive). If the name isn't found in `Clients`, it logs a warning and falls back to a client with empty default settings — it does **not** throw, despite its XML documentation suggesting it validates the name. Double-check the client name if connections are failing unexpectedly.

`EnableLogging` (default `true`) is checked fresh on every log call rather than only at startup — flip it at runtime via `sqlManager.RuntimeSettings.EnableLogging = false;` to silence this module's logging (including every `SQLClient` it hands out) without detaching the `ILoggerFactory` you registered for the rest of the app.

## How to

### Run commands

```csharp
var insertResult = await sqlClient.RunNonQueryCommandAsync(
    "INSERT INTO Orders (Id, Name) VALUES (@id, @name)",
    CommandType.Text,
    parameters =>
    {
        parameters.AddWithValue("@id", 42);
        parameters.AddWithValue("@name", "Widget");
    });

if (!insertResult.MethodSuccess)
{
    logger.LogError(insertResult.Exception, "Insert failed");
}
```

For full control over the `SqlCommand`, use `RunCustomCommandAsync` and work with the command directly in the callback.

### Read rows with `SQLUtils`

For `RunDataReaderAsync`, post-process rows with the `SQLUtils` extension methods rather than hand-rolling column access:

```csharp
await sqlClient.RunDataReaderAsync("SELECT * FROM Orders", CommandType.Text, async reader =>
{
    var rows = await reader.ToListDictionaryAsync();
    foreach (var row in rows)
    {
        var name = row.GetValueOrDefault("Name")?.ToString();
    }
});
```

`ToCsvContent`/`ToCsvContentAsync` strip commas, quotes, and newlines from cell values rather than quote-escaping them — the output is a simplified, lossy CSV, not an RFC 4180-compliant one. Don't use it where exact round-tripping of special characters matters.

### Store credentials in a secret store

```csharp
void SetSecretStore(ISecretStore secretStore);
NullOperationResult SetSecretStoreCredentials(string connectionString);
```

Attaching a secret store overrides the plain `ConnectionString` configured on `SQLClientSettings`:

```csharp
var sqlClient = Service_SQLMgmt.SQLManager.GetClient("my-client");
sqlClient.SetSecretStore(Service_CredentialMgmt.FileSecretStore);
sqlClient.SetSecretStoreCredentials("Server=...;Database=...;");
```

This is entirely opt-in, and calling `SetSecretStoreCredentials` before `SetSecretStore` throws (since there's nowhere to store the value yet). See [Credential Management](https://github.com/AndrewM521/AndrewM5.DevKit/blob/main/docs/CredentialMgmt/README.md#wire-into-sqlclient) for details.

## API reference

### `Service_SQLMgmt` (static)

```csharp
public static IServiceCollection AddSQLMgmt(this IServiceCollection services, IConfiguration configuration);
public static void Initialize(IServiceProvider sp);
public static SQLManager SQLManager { get; }   // throws InvalidOperationException before Initialize
```

### `SQLClient`

This module has no pluggable extension point, so there's no `Interfaces/`/`Abstractions/`/`Implementations/` split — `SQLManager` and `SQLClient` are plain classes directly under `Integration.DevKit.SQLMgmt` (settings live in `Integration.DevKit.SQLMgmt.Settings`).

```csharp
public class SQLClient : IDisposable
{
    SQLClientSettings RuntimeSettings { get; set; }
    string ClientName { get; set; }

    Task<OperationResult<bool>> TestSqlConnectionAsync(CancellationToken cancellationToken = default);

    Task<NullOperationResult> RunCustomCommandAsync(string sqlStatement, CommandType commandType,
        Func<SqlCommand, Task> processCommand, int commandTimeoutSeconds = 30, CancellationToken cancellationToken = default);

    Task<OperationResult<int>> RunNonQueryCommandAsync(string sqlStatement, CommandType commandType,
        Action<SqlParameterCollection>? configureParameters = null, int commandTimeoutSeconds = 30, CancellationToken cancellationToken = default);

    Task<NullOperationResult> RunDataReaderAsync(string sqlStatement, CommandType commandType, Func<SqlDataReader, Task> processReader,
        Action<SqlParameterCollection>? configureParameters = null, int commandTimeoutSeconds = 30, CancellationToken cancellationToken = default);

    // TestSqlConnection / RunCustomCommand / RunNonQueryCommand / RunDataReader:
    // synchronous equivalents (each simply blocks on the async version)

    void SetSecretStore(ISecretStore secretStore);
    NullOperationResult SetSecretStoreCredentials(string connectionString);
    NullOperationResult DeleteCredential(string key);
    NullOperationResult DeleteAllCredentials();
    void LogRuntimeSettings(bool calledFromManager = false);
}
```

### `SQLUtils` (extension methods on `DbDataReader`)

```csharp
public static class SQLUtils
{
    object GetDBNullIfNull(object? value, object? nullEquivalentValue = null);
    T GetValueOrDefault<T>(this DbDataReader reader, string columnName, T defaultValue);
    Dictionary<string, object?> RowToDictionary(this DbDataReader reader);
    Task<Dictionary<string, object?>> RowToDictionaryAsync(this DbDataReader reader, CancellationToken cancellationToken = default);
    List<Dictionary<string, object?>> ToListDictionary(this DbDataReader reader);
    Task<List<Dictionary<string, object?>>> ToListDictionaryAsync(this DbDataReader reader, CancellationToken cancellationToken = default);
    List<string[]> ToCsvContent(this DbDataReader reader, List<string>? columnsToInclude = null);
    Task<List<string[]>> ToCsvContentAsync(this DbDataReader reader, List<string>? columnsToInclude = null, CancellationToken cancellationToken = default);
}
```

## Error handling

SQL operations report failure through the SDK's result-object pattern (`OperationResult<T>` / `NullOperationResult`) rather than throwing — check `MethodSuccess` and inspect `Exception` on failure.

## Best practices

- Prefer `UseSingleConnection = true` only when connection setup is genuinely expensive for your provider — it serializes every call on that client behind one connection.
- Don't assume `GetClient` validates the client name — verify configured client names match what you request, since a miss silently falls back to default settings.
- Keep connection strings in a protected configuration store or secret store rather than plain `appsettings.json`.
- Use [`ThreadSafeFileIO`](https://github.com/AndrewM521/AndrewM5.DevKit/blob/main/docs/ThreadSafeItems/README.md) if you're also writing query results out to a shared file — SQL client access itself is already safe for concurrent use per-client.

## Changelog

See [CHANGELOG.md](https://github.com/AndrewM521/AndrewM5.DevKit/blob/main/docs/SQLMgmt/CHANGELOG.md).
