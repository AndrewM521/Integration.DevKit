using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.SqlManagement.Abstractions.Settings;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AndrewM5.DevKit.SqlManagement.Abstractions;

public interface ISqlDBClient : IDisposable
{
    public SqlDBClientSettings RuntimeSettings { get; set; }

    public string ClientName { get; set; }

    #region Asynchronous Methods
    public Task<OperationResult<bool>> TestSqlConnectionAsync(CancellationToken cancellationToken = default);
    
    public Task<OperationResult<int>> RunNonQueryCommandAsync(string sqlStatement, CommandType commandType, SqlParameter[]? parameters = null, CancellationToken cancellationToken = default);

    public Task<OperationResult<int>> RunNonQueryCommandAsync(string sqlStatement, CommandType commandType, Func<SqlCommand, Task<int>> processCommand, CancellationToken cancellationToken = default);

    public Task<OperationResult<T>> RunDataReaderAsync<T>(string sqlStatement, CommandType commandType, Func<SqlDataReader, Task<T>> processReader, SqlParameter[]? parameters = null, CancellationToken cancellationToken = default);
    #endregion

    #region Synchronous Methods
    public OperationResult<bool> TestSqlConnection();
    
    public OperationResult<int> RunNonQueryCommand(string sqlStatement, CommandType commandType, SqlParameter[]? parameters = null);

    public OperationResult<int> RunNonQueryCommand(string sqlStatement, CommandType commandType, Func<SqlCommand, int> processCommand);

    public OperationResult<T> RunDataReader<T>(string sqlStatement, CommandType commandType, Func<SqlDataReader, T> processReader, SqlParameter[]? parameters = null);
    #endregion

    public void OutputRuntimeSettings(bool calledFromManager = false);
}
