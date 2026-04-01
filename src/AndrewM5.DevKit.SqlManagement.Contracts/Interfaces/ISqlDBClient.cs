using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.CredentialManagement.Contracts.Interfaces;
using AndrewM5.DevKit.SqlManagement.Abstractions.Options;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AndrewM5.DevKit.SqlManagement.Contracts.Interfaces;

public interface ISqlDBClient : IDisposable
{
    public SqlDBClientSettings RuntimeSettings { get; set; }

    public string ClientName { get; set; }

    #region Asynchronous Methods
    public Task<OperationResult<bool>> TestSqlConnectionAsync(CancellationToken cancellationToken = default);
    
    public Task<OperationResult<int>> RunNonQueryCommandAsync(string sqlStatement, CommandType commandType, Action<SqlParameterCollection>? configureParameters = null, CancellationToken cancellationToken = default);

    public Task<NullOperationResult> RunNonQueryCommandAsync(string sqlStatement, CommandType commandType, Func<SqlCommand, Task> processCommand, CancellationToken cancellationToken = default);

    public Task<NullOperationResult> RunDataReaderAsync(string sqlStatement, CommandType commandType, Func<SqlDataReader, Task> processReader, Action<SqlParameterCollection>? configureParameters = null, CancellationToken cancellationToken = default);
    #endregion

    #region Synchronous Methods
    public OperationResult<bool> TestSqlConnection();
    
    public OperationResult<int> RunNonQueryCommand(string sqlStatement, CommandType commandType, Action<SqlParameterCollection>? configureParameters = null);

    public NullOperationResult RunNonQueryCommand(string sqlStatement, CommandType commandType, Func<SqlCommand, Task> processCommand);

    public NullOperationResult RunDataReader(string sqlStatement, CommandType commandType, Func<SqlDataReader, Task> processReader, Action<SqlParameterCollection>? configureParameters = null);
    #endregion

    #region Credentials
    public void SetSecretStore(ISecretStore secretStore);
    
    public NullOperationResult SetCredentials(string server, string database, string username, string password);

    public NullOperationResult DeleteCredential(string key);

    public NullOperationResult DeleteAllCredentials();
    #endregion

    public void OutputRuntimeSettings(bool calledFromManager = false);
}
