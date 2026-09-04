/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Reflection;
using Integration.DevKit.Core;
using Integration.DevKit.CredentialMgmt.Contracts;
using Integration.DevKit.SQLMgmt.Settings;

namespace Integration.DevKit.SQLMgmt;

/// <summary>
/// SQL Database Client capable of executing commands,
/// managing credentials, and testing connectivity both synchronously and asynchronously.
/// </summary>
public class SQLClient : IDisposable
{
    /// <summary>
    /// Gets or sets the configuration settings used by the client at runtime.
    /// </summary>
    public SQLClientSettings RuntimeSettings { get; set; }

    /// <summary>
    /// Gets or sets the unique name or identifier for this client instance.
    /// </summary>
    /// <value>A string representing the name of the client, used for logging or identification.</value>
    public string ClientName { get; set; }

    private readonly ILogger? _logger;

    private const string NoSecretStore = "SecretStore has not been set. Call SetSecretStore()";
    private const string ConnectionStringKey = "ConnectionString";

    private readonly string _secretStoreFileName;
    private ISecretStore? _secretStore;
     
    private SqlConnection? _mainSqlConnection;
    private readonly SemaphoreSlim _connectionLock = new SemaphoreSlim(1, 1);

    /// <summary>
    /// Initializes a new instance of the <see cref="SQLClient"/> class.
    /// </summary>
    /// <param name="clientName">The unique name identifying this specific client instance.</param>
    /// <param name="settings">The configuration and connectivity settings.</param>
    /// <param name="logger">An optional logger instance for diagnostic reporting.</param>
    internal SQLClient(string clientName, SQLClientSettings settings, ILogger? logger = null)
    {
        ClientName = clientName;

        _secretStoreFileName = $"ApiClient({ClientName})";
        _logger = logger;

        RuntimeSettings = settings;
    }

    /// <summary>
    /// Injects a secret store implementation to be used for secure credential retrieval.
    /// </summary>
    /// <param name="secretStore">The implementation of <see cref="ISecretStore"/> to use.</param>
    public void SetSecretStore(ISecretStore secretStore)
    {
        _secretStore = secretStore;
    }

    #region Asyncronous Methods
    /// <summary>
    /// Asynchronously tests the connection to the SQL server using the current <see cref="RuntimeSettings"/>.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>An <see cref="OperationResult{T}"/> indicating success and containing a boolean result where <see langword="true"/> means the connection is valid.</returns>
    public async Task<OperationResult<bool>> TestSqlConnectionAsync(CancellationToken cancellationToken = default)
    {
        var result = new OperationResult<bool>();

        var getConnection = GetConnection();
        if (!getConnection.MethodSuccess)
        {
            return result.SetMethodFailure(getConnection.Exception);
        }

        var conn = getConnection.Result;
        bool wasAlreadyOpen = conn.State == ConnectionState.Open;
        
        bool lockAcquired = false;
        if (RuntimeSettings.UseSingleConnection)
        {
            // Wait for the previous query to finish before jumping in
            await _connectionLock.WaitAsync(cancellationToken);
            lockAcquired = true;
        }

        try
        {
            // Only open it if it's currently closed
            if (!wasAlreadyOpen)
            {
                await conn.OpenAsync(cancellationToken);
            }

            // If we just want to verify it works, executing a tiny query 
            // ensures the server is actually responding, not just pooled.
            await using var cmd = new SqlCommand("SELECT 1;", conn);
            await cmd.ExecuteScalarAsync(cancellationToken);

            // If it was already open, leave it open 
            // Otherwise, close it back down if we are pooling.
            if (!wasAlreadyOpen && !RuntimeSettings.UseSingleConnection)
            {
                await conn.CloseAsync();
            }

            return result.SetMethodSuccess(true);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
        finally 
        { 
            if (!RuntimeSettings.UseSingleConnection)
            {
                await conn.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Asynchronously executes a Custom SQL statement, providing direct access to the <see cref="SqlCommand"/> for custom processing.
    /// </summary>
    /// <param name="sqlStatement">The SQL text or stored procedure name to execute.</param>
    /// <param name="commandType">Specifies how the <paramref name="sqlStatement"/> is interpreted.</param>
    /// <param name="processCommand">An asynchronous function to process the <see cref="SqlCommand"/> before or during execution.</param>
    /// <param name="commandTimeoutSeconds">An optional command timeout in seconds. Defaults to 30 seconds</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating the success or failure of the operation.</returns>
    public async Task<NullOperationResult> RunCustomCommandAsync(string sqlStatement, CommandType commandType, Func<SqlCommand, Task> processCommand,
            int commandTimeoutSeconds = 30, CancellationToken cancellationToken = default)
    {
        var result = new NullOperationResult();

        var getConnection = GetConnection();
        if (!getConnection.MethodSuccess)
        {
            return result.SetMethodFailure(getConnection.Exception);
        }

        var conn = getConnection.Result;

        await using var cmd = new SqlCommand(sqlStatement, conn)
        {
            CommandType = commandType
        };

        cmd.CommandTimeout = commandTimeoutSeconds;

        bool lockAcquired = false;
        if (RuntimeSettings.UseSingleConnection)
        {
            // Wait for the previous query to finish before jumping in
            await _connectionLock.WaitAsync(cancellationToken);
            lockAcquired = true;
        }

        try
        {
            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync(cancellationToken);
            }

            await processCommand(cmd).ConfigureAwait(false);

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
        finally
        {
            cmd.Parameters.Clear();

            if (!RuntimeSettings.UseSingleConnection)
            {
                await conn.DisposeAsync();
            }

            if (lockAcquired)
            {
                _connectionLock.Release();
            }

            await GCManager.CallGC_Collect("SQL NonQuery Command");
        }
    }

    /// <summary>
    /// Asynchronously executes a Non-Query SQL statement and returns the number of rows affected.
    /// </summary>
    /// <param name="sqlStatement">The SQL text or stored procedure name to execute.</param>
    /// <param name="commandType">Specifies how the <paramref name="sqlStatement"/> is interpreted (e.g., Text or StoredProcedure).</param>
    /// <param name="configureParameters">An optional action to populate the <see cref="SqlParameterCollection"/> before execution.</param>
    /// <param name="commandTimeoutSeconds">An optional command timeout in seconds. Defaults to 30 seconds</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the number of rows affected.</returns>
    public async Task<OperationResult<int>> RunNonQueryCommandAsync(string sqlStatement, CommandType commandType,
        Action<SqlParameterCollection>? configureParameters = null, int commandTimeoutSeconds = 30, CancellationToken cancellationToken = default)
    {
        var result = new OperationResult<int>();

        var getConnection = GetConnection();
        if (!getConnection.MethodSuccess)
        {
            return result.SetMethodFailure(getConnection.Exception);
        }

        var conn = getConnection.Result;

        await using var cmd = new SqlCommand(sqlStatement, conn)
        {
            CommandType = commandType
        };

        cmd.CommandTimeout = commandTimeoutSeconds;

        bool lockAcquired = false;
        if (RuntimeSettings.UseSingleConnection)
        {
            // Wait for the previous query to finish before jumping in
            await _connectionLock.WaitAsync(cancellationToken);
            lockAcquired = true;
        }

        try
        {
            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync(cancellationToken);
            }

            configureParameters?.Invoke(cmd.Parameters);

            int count = await cmd.ExecuteNonQueryAsync(cancellationToken);

            return result.SetMethodSuccess(count);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
        finally
        {
            cmd.Parameters.Clear();

            if (!RuntimeSettings.UseSingleConnection)
            {
                await conn.DisposeAsync();
            }

            if (lockAcquired)
            {
                _connectionLock.Release();
            }

            await GCManager.CallGC_Collect("SQL NonQuery Command");
        }
    }

    /// <summary>
    /// Asynchronously executes a query and provides a <see cref="SqlDataReader"/> to a callback for processing results.
    /// </summary>
    /// <param name="sqlStatement">The SQL query to execute.</param>
    /// <param name="commandType">Specifies how the <paramref name="sqlStatement"/> is interpreted.</param>
    /// <param name="processReader">An asynchronous function to handle the data reading logic using the provided <see cref="SqlDataReader"/>.</param>
    /// <param name="configureParameters">An optional action to populate the <see cref="SqlParameterCollection"/>.</param>
    /// <param name="commandTimeoutSeconds">An optional command timeout in seconds. Defaults to 30 seconds</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating the success or failure of the operation.</returns>
    public async Task<NullOperationResult> RunDataReaderAsync(string sqlStatement, CommandType commandType, Func<SqlDataReader, Task> processReader,
        Action<SqlParameterCollection>? configureParameters = null, int commandTimeoutSeconds = 30, CancellationToken cancellationToken = default)
    {
        var result = new NullOperationResult();

        var getConnection = GetConnection();
        if (!getConnection.MethodSuccess)
        {
            return result.SetMethodFailure(getConnection.Exception);
        }

        var conn = getConnection.Result;

        await using var cmd = new SqlCommand(sqlStatement, conn)
        {
            CommandType = commandType
        };

        cmd.CommandTimeout = commandTimeoutSeconds;

        bool lockAcquired = false;
        if (RuntimeSettings.UseSingleConnection)
        {
            // Wait for the previous query to finish before jumping in
            await _connectionLock.WaitAsync(cancellationToken);
            lockAcquired = true;
        }

        try
        {
            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync(cancellationToken);
            }

            configureParameters?.Invoke(cmd.Parameters);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            // The inner processReader method exists so that the caller access to the reader but not ownership of it.
            // The reader is then disposed automatically when the callback completes ensuring the reader is released,
            // preventing leaks.

            await processReader(reader).ConfigureAwait(false);

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
        finally
        {
            cmd.Parameters.Clear();

            if (!RuntimeSettings.UseSingleConnection)
            {
                await conn.DisposeAsync();
            }
            
            if (lockAcquired)
            {
                _connectionLock.Release();
            }

            await GCManager.CallGC_Collect("SQL Data Reader");
        }
    }
    #endregion

    #region Syncronous Methods
    /// <summary>
    /// Synchronously tests the connection to the SQL server.
    /// </summary>
    /// <returns>An <see cref="OperationResult{T}"/> containing a boolean indicating if the connection was successful.</returns>
    public OperationResult<bool> TestSqlConnection()
    {
        return TestSqlConnectionAsync().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Synchronously executes a custom SQL statement, providing direct access to the <see cref="SqlCommand"/> via a callback.
    /// </summary>
    /// <param name="sqlStatement">The SQL text or stored procedure name to execute.</param>
    /// <param name="commandType">Specifies how the <paramref name="sqlStatement"/> is interpreted.</param>
    /// <param name="processCommand">An asynchronous function (invoked synchronously) to process the <see cref="SqlCommand"/>.</param>
    /// <param name="commandTimeoutSeconds">An optional command timeout in seconds. Defaults to 30 seconds</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating success or failure.</returns>
    public NullOperationResult RunCustomCommand(string sqlStatement, CommandType commandType,
        Func<SqlCommand, Task> processCommand, int commandTimeoutSeconds = 30)
    {
        return RunCustomCommandAsync(sqlStatement, commandType, command => processCommand(command)).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Synchronously executes a Non-Query SQL statement and returns the number of rows affected.
    /// </summary>
    /// <param name="sqlStatement">The SQL text or stored procedure name to execute.</param>
    /// <param name="commandType">Specifies how the <paramref name="sqlStatement"/> is interpreted.</param>
    /// <param name="configureParameters">An optional action to populate the <see cref="SqlParameterCollection"/>.</param>
    /// <param name="commandTimeoutSeconds">An optional command timeout in seconds. Defaults to 30 seconds</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the number of rows affected.</returns>
    public OperationResult<int> RunNonQueryCommand(string sqlStatement, CommandType commandType,
        Action<SqlParameterCollection>? configureParameters = null, int commandTimeoutSeconds = 30)
    {
        return RunNonQueryCommandAsync(sqlStatement, commandType, configureParameters).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Synchronously executes a query and provides a <see cref="SqlDataReader"/> to a callback for processing.
    /// </summary>
    /// <param name="sqlStatement">The SQL query to execute.</param>
    /// <param name="commandType">Specifies how the <paramref name="sqlStatement"/> is interpreted.</param>
    /// <param name="processReader">An asynchronous function (invoked synchronously) to handle the data reading logic.</param>
    /// <param name="configureParameters">An optional action to populate the <see cref="SqlParameterCollection"/>.</param>
    /// <param name="commandTimeoutSeconds">An optional command timeout in seconds. Defaults to 30 seconds</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating success or failure.</returns>
    public NullOperationResult RunDataReader(string sql, CommandType commandType, Func<SqlDataReader, Task> processReader,
        Action<SqlParameterCollection>? configureParameters = null, int commandTimeoutSeconds = 30)
    {
        return RunDataReaderAsync(sql, commandType, 
            reader => processReader(reader), configureParameters).GetAwaiter().GetResult();
    }
    #endregion

    #region Credentials
    /// <summary>
    /// Manually sets the connection string for the client.
    /// </summary>
    /// <param name="connectionString">The full SQL connection string.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating if the connection string was successfully applied.</returns>
    public NullOperationResult SetSecretStoreCredentials(string connectionString)
    {
        var result = new NullOperationResult();

        try
        {
            if (_secretStore == null)
            {
                throw new ArgumentNullException(NoSecretStore);
            }

            var setServerKey = _secretStore.SetKey(_secretStoreFileName, ConnectionStringKey, connectionString);
            if (!setServerKey.MethodSuccess)
            {
                throw setServerKey.Exception;
            }

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Deletes a specific credential from the store based on the provided key.
    /// </summary>
    /// <param name="key">The key identifying the credential to remove.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating the result of the deletion.</returns>
    public NullOperationResult DeleteCredential(string key)
    {
        var result = new NullOperationResult();

        try
        {
            if (_secretStore == null)
            {
                throw new ArgumentNullException(NoSecretStore);
            }

            return _secretStore.DeleteKey(_secretStoreFileName, key);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Removes all stored credentials associated with this client instance.
    /// </summary>
    /// <returns>A <see cref="NullOperationResult"/> indicating the result of the deletion.</returns>
    public NullOperationResult DeleteAllCredentials()
    {
        var result = new NullOperationResult();

        try
        {
            if (_secretStore == null)
            {
                throw new ArgumentNullException(NoSecretStore);
            }

            return _secretStore.DeleteSecret(_secretStoreFileName);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Internal helper to resolve a specific credential key from the secret store.
    /// </summary>
    /// <param name="key">The specific key to retrieve.</param>
    /// <param name="defaultStr">The fallback value if the secret store is not available.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the resolved string.</returns>
    private OperationResult<string> GetCredentials(string key, string defaultStr)
    {
        var result = new OperationResult<string>();

        try
        {
            if (_secretStore != null)
            {
                var getKey = _secretStore.GetKey(_secretStoreFileName, key);
                if (!getKey.MethodSuccess)
                {
                    throw getKey.Exception;
                }

                return result.SetMethodSuccess(getKey.Result);
            }
            else
            {
                return result.SetMethodSuccess(defaultStr);
            }
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }
    #endregion

    /// <summary>
    /// Retrieves a SQL connection instance. 
    /// Depending on <see cref="SQLClientSettings.UseSingleConnection"/>, this either returns a 
    /// cached instance or a new connection object.
    /// </summary>
    /// <returns>An <see cref="OperationResult{T}"/> containing the <see cref="SqlConnection"/>.</returns>
    private OperationResult<SqlConnection> GetConnection()
    {
        var result = new OperationResult<SqlConnection>();

        try
        {
            if (RuntimeSettings.UseSingleConnection)
            {
                if (_mainSqlConnection != null && (_mainSqlConnection.State == ConnectionState.Broken || _mainSqlConnection.State == ConnectionState.Closed))
                {
                    _mainSqlConnection.Dispose();
                    _mainSqlConnection = null;
                }

                if (_mainSqlConnection == null)
                {
                    var getConnectionStr = GetConnectionString();
                    if (!getConnectionStr.MethodSuccess)
                    {
                        throw getConnectionStr.Exception;
                    }

                    _mainSqlConnection = new SqlConnection(getConnectionStr.Result);
                }

                return result.SetMethodSuccess(_mainSqlConnection);
            }
            else
            {
                var getConnectionStr = GetConnectionString();
                if (!getConnectionStr.MethodSuccess)
                {
                    throw getConnectionStr.Exception;
                }

                return result.SetMethodSuccess(new SqlConnection(getConnectionStr.Result));
            }
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Generates the final connection string by prioritizing the <see cref="_secretStore"/> 
    /// over the hardcoded <see cref="RuntimeSettings"/>.
    /// </summary>
    /// <returns>An <see cref="OperationResult{T}"/> containing the resolved connection string.</returns>
    private OperationResult<string> GetConnectionString()
    {
        var result = new OperationResult<string>();

        try
        {
            var getConnectionString = GetCredentials("ConnectionString", RuntimeSettings.ConnectionString);
            if (!getConnectionString.MethodSuccess)
            {
                throw getConnectionString.Exception;
            }

            return result.SetMethodSuccess(getConnectionString.Result);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Logging method to output current <see cref="SQLClientSettings"/> to the logs.
    /// </summary>
    /// <param name="calledFromManager">Indicates if the call originated from a management orchestrator.</param>
    public void LogRuntimeSettings(bool calledFromManager = false)
    {
        string indent;

        if (!calledFromManager)
        {
            _logger?.LogDebug($"--- SqlDB Client Settings ---");
            indent = "";
        }
        else
        {
            indent = "    ";
        }

        _logger?.LogDebug($"{indent}Client: {ClientName}");

        indent += "  ";

        Type type = RuntimeSettings.GetType();
        PropertyInfo[] properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            object? value = property.GetValue(RuntimeSettings);
            _logger?.LogDebug($"{indent}{property.Name}: {value}");
        }
    }

    /// <summary>
    /// Disposes of any persistent database connections managed by this client.
    /// </summary>
    public void Dispose()
    {
        _mainSqlConnection?.Dispose();
        _connectionLock?.Dispose();
    }
}
