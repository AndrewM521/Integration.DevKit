/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.Core;
using Integration.DevKit.Core.Results;
using Integration.DevKit.CredentialMgmt.Contracts.Interfaces;
using Integration.DevKit.CustomLogger.Contracts.Interfaces;
using Integration.DevKit.SQLMgmt.Contracts.Interfaces;
using Integration.DevKit.SQLMgmt.Contracts.Options;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Reflection;

namespace Integration.DevKit.SQLMgmt;

/// <summary>
/// Concrete Implementation of <see cref="ISQLClient"/>
/// </summary>
public class SQLClient : ISQLClient
{
    /// <inheritdoc/>
    public SQLClientSettings RuntimeSettings { get; set; }

    /// <inheritdoc/>
    public string ClientName { get; set; }

    private readonly ICustomLogger? _logger;

    private const string NoSecretStore = "SecretStore has not been set. Call SetSecretStore()";
    private const string ConnectionStringKey = "ConnectionString";

    private readonly string _secretStoreFileName;
    private ISecretStore? _secretStore;

    private SqlConnection? _mainSqlConnection;

    /// <summary>
    /// Initializes a new instance of the <see cref="SQLClient"/> class.
    /// </summary>
    /// <param name="clientName">The unique name identifying this specific client instance.</param>
    /// <param name="settings">The configuration and connectivity settings.</param>
    /// <param name="logger">An optional logger instance for diagnostic reporting.</param>
    internal SQLClient(string clientName, SQLClientSettings settings, ICustomLogger? logger = null)
    {
        ClientName = clientName;

        _secretStoreFileName = $"ApiClient({ClientName})";
        _logger = logger;

        RuntimeSettings = settings;
    }

    /// <inheritdoc/>
    public void SetSecretStore(ISecretStore secretStore)
    {
        _secretStore = secretStore;
    }

    #region Asyncronous Methods
    /// <inheritdoc/>
    public async Task<OperationResult<bool>> TestSqlConnectionAsync(CancellationToken cancellationToken = default)
    {
        var result = new OperationResult<bool>();

        try
        {
            var getConnection = GetConnection();
            if (!getConnection.MethodSuccess)
            {
                throw getConnection.Exception;
            }

            await using (var conn = getConnection.Result)
            {
                await conn.OpenAsync(cancellationToken);

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
            await GCManager.CallGC_Collect("SQL Test Connection");
        }
    }

    /// <inheritdoc/>
    public async Task<OperationResult<int>> RunNonQueryCommandAsync(string sqlStatement, CommandType commandType, 
        Action<SqlParameterCollection>? configureParameters = null, CancellationToken cancellationToken = default)
    {
        var result = new OperationResult<int>();

        var getConnection = GetConnection();
        if (!getConnection.MethodSuccess)
        {
            throw getConnection.Exception;
        }

        await using (var conn = getConnection.Result)
        {
            await using var cmd = new SqlCommand(sqlStatement, conn)
            {
                CommandType = commandType
            };

            try
            {
                await conn.OpenAsync(cancellationToken);

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

                await GCManager.CallGC_Collect("SQL NonQuery Command");
            }
        }
    }

    /// <inheritdoc/>
    public async Task<NullOperationResult> RunNonQueryCommandAsync(string sqlStatement, CommandType commandType, 
            Func<SqlCommand, Task> processCommand, CancellationToken cancellationToken = default)
    {
        var result = new NullOperationResult();

        var getConnection = GetConnection();
        if (!getConnection.MethodSuccess)
        {
            throw getConnection.Exception;
        }

        await using (var conn = getConnection.Result)
        {
            await using var cmd = new SqlCommand(sqlStatement, conn)
            {
                CommandType = commandType
            };

            try
            {
                await conn.OpenAsync(cancellationToken);

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

                await GCManager.CallGC_Collect("SQL NonQuery Command");
            }
        }
    }

    /// <inheritdoc/>
    public async Task<NullOperationResult> RunDataReaderAsync(string sqlStatement, CommandType commandType, Func<SqlDataReader, Task> processReader,
        Action<SqlParameterCollection>? configureParameters = null, CancellationToken cancellationToken = default)
    {
        var result = new NullOperationResult();

        var getConnection = GetConnection();
        if (!getConnection.MethodSuccess)
        {
            throw getConnection.Exception;
        }

        await using (var conn = getConnection.Result)
        {
            await using var cmd = new SqlCommand(sqlStatement, conn)
            {
                CommandType = commandType
            };

            try
            {
                await conn.OpenAsync(cancellationToken);

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

                await GCManager.CallGC_Collect("SQL Data Reader");
            }
        }
    }
    #endregion

    #region Syncronous Methods
    /// <inheritdoc/>
    public OperationResult<bool> TestSqlConnection()
    {
        return TestSqlConnectionAsync().GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public OperationResult<int> RunNonQueryCommand(string sqlStatement, CommandType commandType, Action<SqlParameterCollection>? configureParameters = null)
    {
        return RunNonQueryCommandAsync(sqlStatement, commandType, configureParameters).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public NullOperationResult RunNonQueryCommand(string sqlStatement, CommandType commandType, Func<SqlCommand, Task> processCommand)
    {
        return RunNonQueryCommandAsync(sqlStatement, commandType, 
            command => processCommand(command)).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    public NullOperationResult RunDataReader(string sql, CommandType commandType,
        Func<SqlDataReader, Task> processReader, Action<SqlParameterCollection>? configureParameters = null)
    {
        return RunDataReaderAsync(sql, commandType, 
            reader => processReader(reader), configureParameters).GetAwaiter().GetResult();
    }
    #endregion

    #region Credentials
    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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

    /// <inheritdoc/>
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
    }
}
