# Sql Client Management Module: Quick Start
The SQL Management module provides a robust, factory-based approach to handling SQL database interactions. It consists of the SqlDBManager, 
which handles the lifecycle and configuration of database connections, and the SqlDBClient, which executes the commands.

## 1. Access and Configuration 
The SQL module relies on the standard .NET Options pattern for configuration. It maps settings from your appsettings.json to the manager's runtime state.

Configuration (appsettings.json)
Define your named database clients in the SqlClientManagement section:

```
"SqlClientManagement": {
  "Clients": {
    "TestClient": {
      "ConnectionString": "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;",
      "UseSingleConnection": true
    },
    "TestClient2": {
      "ConnectionString": "Server=myServerAddress;Database=myDataBase;User Id=myUsername;Password=myPassword;",
      "UseSingleConnection": true
    }
  }
}
```

Key Features: ISecretStore integration for encrypted connection strings, and a callback-based DataReader to prevent connection leaks.

Namespace: AndrewM5.DevKit.SqlManagement

Thread Safety: Clients are stored in a ConcurrentDictionary, ensuring that GetClient calls are thread-safe and existing clients are reused (Flyweight pattern).
Runtime Flexibility: You can inspect or modify RuntimeSettings at execution time if necessary.

## 2. Setup
The Sql Client Management module follows the same host-based initialization pattern as the rest of the DevKit.
This involves registering the service and then initializing the static host provider.

Registration and Initalization
1. Add the SQL Management services to your IServiceCollection:
```
.ConfigureServices((context, services) =>
{
    // ... other services
    services.AddSqlManagement(config); 
})
```

2. After building the host, Initialize the SqlManagementHost to enable access throughout your application:
```
var host = builder.Build();

SqlManagementHost.Initialize(host.Services);
```

## 3. Examples
Once configured, you can retrieve specific database clients by the names defined in your configuration.

Retrieving a Configured Client
Access the manager through the host to get a specific database client.

```
// Get the client defined in appsettings.json
var client = SqlManagementHost.SqlManager.GetClient("TestClient");

// If you request a client name that isn't configured, 
// it will return a client with default settings and log a warning.
var defaultClient = SqlManagementHost.SqlManager.GetClient("UnconfiguredClient");
```

Executing Non-Query Commands
The RunNonQueryCommandAsync method uses a callback. Your logic stays inside the block, and the module ensures the command is closed immediately afterward.
```
string sql = "UPDATE Inventory SET Stock = @qty WHERE ItemId = @id";

var result = client.RunNonQueryCommandAsync(sql, CommandType.Text, p => 
{
    p.AddWithValue("@qty", 50);
    p.AddWithValue("@id", 101);
});
```

Executing Query (DataReader) Commands
The RunDataReaderAsync method uses a callback. Your logic stays inside the block, and the module ensures the reader and command is closed immediately afterward.
```
string sql = "SELECT ItemName FROM Inventory";

await client.RunDataReaderAsync(sql, CommandType.Text, async reader => 
{
    while (await reader.ReadAsync())
    {
        Console.WriteLine(reader["ItemName"]);
    }
});
```

Secure Credential Management
You can set the connection string using the encrypted secret store at runtime, which will then take priority over the appsettings.json value.
```
var client = ApiManagementHost.SqlManager.GetClient("ProductionDB");
client.SetSecretStoreCredentials("Server=SecureSrv;Database=Prod;User Id=...;");
```

## [NOTE]
Automatic Cleanup: SqlDBManager implements IAsyncDisposable. When the application host shuts down, the manager automatically disposes of 
all active ISqlDBClient instances, ensuring database connections are closed gracefully.
Logging: 
1. Manager - Use ```ApiManagementHost.SqlManager.LogRuntimeSettings();``` to print a detailed report of all clients, and their active configurations to your debug logs.
2. Client - Use ```client.LogRuntimeSettings();``` to print a detailed report of the clients active configuration to your debug logs.