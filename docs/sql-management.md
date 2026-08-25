# SQL Management

`Integration.DevKit.SQLMgmt` provides named SQL Server client management: connection testing, and command/data-reader execution helpers, all returning the SDK's shared result-object types instead of throwing on failure.

## Requirements

- .NET 8
- `Microsoft.Data.SqlClient`
- Network access to the target SQL Server

## Installation

```bash
dotnet add reference src/Integration.DevKit.SQLMgmt/Integration.DevKit.SQLMgmt.csproj
```

Or from NuGet: [Integration.DevKit.SQLMgmt](https://www.nuget.org/packages/Integration.DevKit.SQLMgmt)

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

`SQLManager.GetClient(clientName)` caches one `ISQLClient` per name (case-insensitive). If the name isn't found in `Clients`, it logs a warning and falls back to a client with empty default settings — it does **not** throw, despite the interface's XML documentation suggesting it validates the name. Double-check the client name if connections are failing unexpectedly.

## `ISQLClient`

```csharp
public interface ISQLClient : IDisposable
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

For `RunDataReaderAsync`, post-process rows with the `SQLUtils` extension methods described below rather than hand-rolling column access.

## `SQLUtils` (extension methods on `DbDataReader`)

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

## Credentials

```csharp
void SetSecretStore(ISecretStore secretStore);
NullOperationResult SetSecretStoreCredentials(string connectionString);
```

As with `ApiClient`, attaching a secret store overrides the plain `ConnectionString` configured on `SQLClientSettings`. See [Credential Management](credential-management.md).

## API Reference

### `Service_SQLMgmt` (static)

```csharp
public static IServiceCollection AddSQLMgmt(this IServiceCollection services, IConfiguration configuration);
public static void Initialize(IServiceProvider sp);
public static ISQLManager SQLManager { get; }   // throws InvalidOperationException before Initialize
```

## Error Handling

SQL operations report failure through the SDK's result-object pattern (`OperationResult<T>` / `NullOperationResult`) rather than throwing — check `MethodSuccess` and inspect `Exception` on failure.

## Best Practices

- Prefer `UseSingleConnection = true` only when connection setup is genuinely expensive for your provider — it serializes every call on that client behind one connection.
- Don't assume `GetClient` validates the client name — verify configured client names match what you request, since a miss silently falls back to default settings.
- Keep connection strings in a protected configuration store or secret store rather than plain `appsettings.json`.
- Use [`ThreadSafeFileIO`](thread-locks.md#thread-safe-file-io) if you're also writing query results out to a shared file — SQL client access itself is already safe for concurrent use per-client.
