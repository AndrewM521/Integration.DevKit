using AndrewM5.DevKit.Core;
using AndrewM5.DevKit.Core.Results;
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

    public SqlDBClient(ISqlDBManager sqlDBManager, string clientName, SqlDBClientSettings settings, ICustomLogger? logger = null)
    {
        _logger = logger;
        ClientName = clientName;

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

    #region Asyncronous Methods
    public async Task<OperationResult<bool>> TestSqlConnectionAsync(CancellationToken cancellationToken = default)
    {
        var result = new OperationResult<bool>();

        await _rateLimiter.WaitAsync(cancellationToken);

        try
        {
            await using (var conn = new SqlConnection(GetConnectionString()))
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

    public async Task<OperationResult<int>> RunNonQueryCommandAsync(string sqlStatement, CommandType commandType, SqlParameter[]? parameters = null, CancellationToken cancellationToken = default)
    {
        var result = new OperationResult<int>();

        await _rateLimiter.WaitAsync(cancellationToken);

        await using var conn = new SqlConnection(GetConnectionString());

        await using var cmd = new SqlCommand(sqlStatement, conn)
        {
            CommandType = commandType,
            CommandTimeout = (int)RuntimeSettings.CommandTimeoutSeconds!
        };

        try
        {
            await conn.OpenAsync(cancellationToken);

            if (parameters != null && parameters.Length > 0)
            {
                cmd.Parameters.AddRange(parameters);
            }

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

        await using var conn = new SqlConnection(GetConnectionString());

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
        SqlParameter[]? parameters = null, CancellationToken cancellationToken = default)
    {
        var result = new OperationResult<T>();

        await _rateLimiter.WaitAsync(cancellationToken);

        await using var conn = new SqlConnection(GetConnectionString());

        await using var cmd = new SqlCommand(sqlStatement, conn)
        {
            CommandType = commandType,
            CommandTimeout = (int)RuntimeSettings.CommandTimeoutSeconds!
        };

        try
        {
            await conn.OpenAsync(cancellationToken);

            if (parameters != null && parameters.Length > 0)
            {
                cmd.Parameters.AddRange(parameters);
            }

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

    public OperationResult<int> RunNonQueryCommand(string sqlStatement, CommandType commandType, SqlParameter[]? parameters = null)
    {
        return RunNonQueryCommandAsync(sqlStatement, commandType, parameters).GetAwaiter().GetResult();
    }

    public OperationResult<int> RunNonQueryCommand(string sqlStatement, CommandType commandType, Func<SqlCommand, int> processCommand)
    {
        return RunNonQueryCommandAsync(sqlStatement, commandType, 
            command => Task.FromResult(processCommand(command))).GetAwaiter().GetResult();
    }

    public OperationResult<T> RunDataReader<T>(string sql, CommandType commandType,
        Func<SqlDataReader, T> processReader, SqlParameter[]? parameters = null)
    {
        return RunDataReaderAsync(sql, commandType, 
            reader => Task.FromResult(processReader(reader)), parameters).GetAwaiter().GetResult();
    }
    #endregion

    private string GetConnectionString()
    {
        return @$"
            Server={RuntimeSettings.Server};
            Database={RuntimeSettings.Database};
            User Id={RuntimeSettings.Username};
            Password={RuntimeSettings.Password};
            MultipleActiveResultSets={RuntimeSettings.MultipleActiveResultSets};
            TrustServerCertificate={RuntimeSettings.TrustServerCertificate};
            Connect Timeout={RuntimeSettings.ConnectionTimeoutSeconds};
        ";
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
