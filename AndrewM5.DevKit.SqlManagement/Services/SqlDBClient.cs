using AndrewM5.DevKit.Core;
using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.CredentialManagement.Abstractions;
using AndrewM5.DevKit.Logging.Abstractions;
using AndrewM5.DevKit.SqlManagement.Abstractions;
using AndrewM5.DevKit.SqlManagement.Abstractions.Settings;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Reflection;

namespace AndrewM5.DevKit.SqlManagement.Services;

public class SqlDBClient : ISqlDBClient
{
    public SqlDBClientSettings RuntimeSettings { get; set; }

    public string ClientName { get; set; }

    private readonly SemaphoreSlim _rateLimiter;
    private readonly ICustomLogger? _logger;

    private const string NoSecretStore = "SecretStore has not been set. Call SetSecretStore()";
    private readonly string _secretStoreFileName;
    private ISecretStore? _secretStore;

    public SqlDBClient(ISqlDBManager sqlDBManager, string clientName, SqlDBClientSettings settings, ICustomLogger? logger = null)
    {
        ClientName = clientName;

        _secretStoreFileName = $"ApiClient({ClientName})";
        _logger = logger;

        RuntimeSettings = settings;

        if (RuntimeSettings.MaxConcurrentQueries < 0)
        {
            RuntimeSettings.MaxConcurrentQueries = int.MaxValue;
        }

        if (RuntimeSettings.CommandTimeoutSeconds != null)
        {
            if (RuntimeSettings.CommandTimeoutSeconds < 0)
            {
                RuntimeSettings.CommandTimeoutSeconds = 0;
            }
        }
        else
        {
            RuntimeSettings.CommandTimeoutSeconds = sqlDBManager.RuntimeSettings.DefaultCommandTimeoutSeconds;
        }

        _rateLimiter = new SemaphoreSlim(RuntimeSettings.MaxConcurrentQueries);
    }

    public void SetSecretStore(ISecretStore secretStore)
    {
        _secretStore = secretStore;
    }

    #region Asyncronous Methods
    public async Task<OperationResult<bool>> TestSqlConnectionAsync(CancellationToken cancellationToken = default)
    {
        var result = new OperationResult<bool>();

        await _rateLimiter.WaitAsync(cancellationToken);

        try
        {
            var getConnectionStr = GetConnectionString();
            if (!getConnectionStr.MethodSuccess)
            {
                throw getConnectionStr.Exception;
            }

            await using (var conn = new SqlConnection(getConnectionStr.Result))
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

            _rateLimiter.Release();
        }
    }

    public async Task<OperationResult<int>> RunNonQueryCommandAsync(string sqlStatement, CommandType commandType, Action<SqlParameterCollection>? configureParameters = null, CancellationToken cancellationToken = default)
    {
        var result = new OperationResult<int>();

        await _rateLimiter.WaitAsync(cancellationToken);

        var getConnectionStr = GetConnectionString();
        if (!getConnectionStr.MethodSuccess)
        {
            return result.SetMethodFailure(getConnectionStr.Exception);
        }

        await using var conn = new SqlConnection(getConnectionStr.Result);

        await using var cmd = new SqlCommand(sqlStatement, conn)
        {
            CommandType = commandType,
            CommandTimeout = (int)RuntimeSettings.CommandTimeoutSeconds!
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

            _rateLimiter.Release();
        }
    }

    public async Task<OperationResult<int>> RunNonQueryCommandAsync(string sqlStatement, CommandType commandType, Func<SqlCommand, Task<int>> processCommand, CancellationToken cancellationToken = default)
    {
        var result = new OperationResult<int>();

        await _rateLimiter.WaitAsync(cancellationToken);

        var getConnectionStr = GetConnectionString();
        if (!getConnectionStr.MethodSuccess)
        {
            return result.SetMethodFailure(getConnectionStr.Exception);
        }

        await using var conn = new SqlConnection(getConnectionStr.Result);

        await using var cmd = new SqlCommand(sqlStatement, conn)
        {
            CommandType = commandType,
            CommandTimeout = (int)RuntimeSettings.CommandTimeoutSeconds!
        };

        try
        {
            await conn.OpenAsync(cancellationToken);    

            int count = await processCommand(cmd).ConfigureAwait(false);

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

            _rateLimiter.Release();
        }
    }

    public async Task<OperationResult<T>> RunDataReaderAsync<T>(string sqlStatement, CommandType commandType, Func<SqlDataReader, Task<T>> processReader,
        Action<SqlParameterCollection>? configureParameters = null, CancellationToken cancellationToken = default)
    {
        var result = new OperationResult<T>();

        await _rateLimiter.WaitAsync(cancellationToken);

        var getConnectionStr = GetConnectionString();
        if (!getConnectionStr.MethodSuccess)
        {
            return result.SetMethodFailure(getConnectionStr.Exception);
        }

        await using var conn = new SqlConnection(getConnectionStr.Result);

        await using var cmd = new SqlCommand(sqlStatement, conn)
        {
            CommandType = commandType,
            CommandTimeout = (int)RuntimeSettings.CommandTimeoutSeconds!
        };

        try
        {
            await conn.OpenAsync(cancellationToken);

            configureParameters?.Invoke(cmd.Parameters);

            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

            // The inner processReader method exists so that the caller access to the reader but not ownership of it.
            // The reader is then disposed automatically when the callback completes ensuring the reader is released,
            // preventing leaks.

            T retVal = await processReader(reader).ConfigureAwait(false);

            return result.SetMethodSuccess(retVal);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
        finally
        {
            cmd.Parameters.Clear();

            await GCManager.CallGC_Collect("SQL Data Reader");

            _rateLimiter.Release();
        }
    }
    #endregion

    #region Syncronous Methods
    public OperationResult<bool> TestSqlConnection()
    {
        return TestSqlConnectionAsync().GetAwaiter().GetResult();
    }

    public OperationResult<int> RunNonQueryCommand(string sqlStatement, CommandType commandType, Action<SqlParameterCollection>? configureParameters = null)
    {
        return RunNonQueryCommandAsync(sqlStatement, commandType, configureParameters).GetAwaiter().GetResult();
    }

    public OperationResult<int> RunNonQueryCommand(string sqlStatement, CommandType commandType, Func<SqlCommand, int> processCommand)
    {
        return RunNonQueryCommandAsync(sqlStatement, commandType, 
            command => Task.FromResult(processCommand(command))).GetAwaiter().GetResult();
    }

    public OperationResult<T> RunDataReader<T>(string sql, CommandType commandType,
        Func<SqlDataReader, T> processReader, Action<SqlParameterCollection>? configureParameters = null)
    {
        return RunDataReaderAsync(sql, commandType, 
            reader => Task.FromResult(processReader(reader)), configureParameters).GetAwaiter().GetResult();
    }
    #endregion

    #region Credentials
    public NullOperationResult SetCredentials(string server, string database, string username, string password)
    {
        var result = new NullOperationResult();

        try
        {
            if (_secretStore == null)
            {
                throw new ArgumentNullException(NoSecretStore);
            }

            var setServerKey = _secretStore.SetKey(_secretStoreFileName, "server", username);
            if (!setServerKey.MethodSuccess)
            {
                throw setServerKey.Exception;
            }

            var setDatabaseKey = _secretStore.SetKey(_secretStoreFileName, "database", username);
            if (!setDatabaseKey.MethodSuccess)
            {
                throw setDatabaseKey.Exception;
            }

            var setUsernameKey = _secretStore.SetKey(_secretStoreFileName, "username", username);
            if (!setUsernameKey.MethodSuccess)
            {
                throw setUsernameKey.Exception;
            }

            var setPasswordKey = _secretStore.SetKey(_secretStoreFileName, "password", password);
            if (!setUsernameKey.MethodSuccess)
            {
                throw setUsernameKey.Exception;
            }

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

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

    private OperationResult<string> GetConnectionString()
    {
        var result = new OperationResult<string>();

        try
        {
            var getServer = GetCredentials("server", RuntimeSettings.Server);
            if (!getServer.MethodSuccess)
            {
                throw getServer.Exception;
            }

            var getDatabase = GetCredentials("database", RuntimeSettings.Database);
            if (!getDatabase.MethodSuccess)
            {
                throw getDatabase.Exception;
            }

            var getUsername = GetCredentials("username", RuntimeSettings.Username);
            if (!getUsername.MethodSuccess)
            {
                throw getUsername.Exception;
            }

            var getPassword = GetCredentials("password", RuntimeSettings.Password);
            if (!getPassword.MethodSuccess)
            {
                throw getPassword.Exception;
            }

            string connectionStr = @$"
                Server={RuntimeSettings.Server};
                Database={RuntimeSettings.Database};
                User Id={RuntimeSettings.Username};
                Password={RuntimeSettings.Password};
                MultipleActiveResultSets={RuntimeSettings.MultipleActiveResultSets};
                TrustServerCertificate={RuntimeSettings.TrustServerCertificate};
                Connect Timeout={RuntimeSettings.ConnectionTimeoutSeconds};
            ";

            return result.SetMethodSuccess(connectionStr);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    public void OutputRuntimeSettings(bool calledFromManager = false)
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

    public void Dispose()
    {
        _rateLimiter?.Dispose();
    }
}
