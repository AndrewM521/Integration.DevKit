using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.CredentialManagement.Contracts.Interfaces;
using AndrewM5.DevKit.SqlManagement.Abstractions.Options;
using Microsoft.Data.SqlClient;
using System.Data;

namespace AndrewM5.DevKit.SqlManagement.Contracts.Interfaces;

/// <summary>
/// Defines a contract for a SQL Database Client capable of executing commands, 
/// managing credentials, and testing connectivity both synchronously and asynchronously.
/// </summary>
public interface ISqlDBClient : IDisposable
{
    /// <summary>
    /// Gets or sets the configuration settings used by the client at runtime.
    /// </summary>
    public SqlDBClientSettings RuntimeSettings { get; set; }

    /// <summary>
    /// Gets or sets the unique name or identifier for this client instance.
    /// </summary>
    public string ClientName { get; set; }

    #region Asynchronous Methods
    /// <summary>
    /// Asynchronously tests the connection to the SQL server using current settings.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>An <see cref="OperationResult{T}"/> indicating success and containing a boolean result.</returns>
    public Task<OperationResult<bool>> TestSqlConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously executes a SQL statement and returns the number of rows affected.
    /// </summary>
    /// <param name="sqlStatement">The SQL text or stored procedure name to execute.</param>
    /// <param name="commandType">Specifies how the <paramref name="sqlStatement"/> is interpreted.</param>
    /// <param name="configureParameters">An optional action to populate the <see cref="SqlParameterCollection"/>.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the number of rows affected.</returns>
    public Task<OperationResult<int>> RunNonQueryCommandAsync(string sqlStatement, CommandType commandType, Action<SqlParameterCollection>? configureParameters = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously executes a SQL statement, providing direct access to the <see cref="SqlCommand"/> for custom processing.
    /// </summary>
    /// <param name="sqlStatement">The SQL text or stored procedure name to execute.</param>
    /// <param name="commandType">Specifies how the <paramref name="sqlStatement"/> is interpreted.</param>
    /// <param name="processCommand">An asynchronous function to process the command before or during execution.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating the success or failure of the operation.</returns>
    public Task<NullOperationResult> RunNonQueryCommandAsync(string sqlStatement, CommandType commandType, Func<SqlCommand, Task> processCommand, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously executes a query and provides a <see cref="SqlDataReader"/> to a callback for processing results.
    /// </summary>
    /// <param name="sqlStatement">The SQL query to execute.</param>
    /// <param name="commandType">Specifies how the <paramref name="sqlStatement"/> is interpreted.</param>
    /// <param name="processReader">An asynchronous function to handle the data reading logic.</param>
    /// <param name="configureParameters">An optional action to populate the <see cref="SqlParameterCollection"/>.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating the success or failure of the operation.</returns>
    public Task<NullOperationResult> RunDataReaderAsync(string sqlStatement, CommandType commandType, Func<SqlDataReader, Task> processReader, Action<SqlParameterCollection>? configureParameters = null, CancellationToken cancellationToken = default);
    #endregion

    #region Synchronous Methods
    /// <summary>
    /// Synchronously tests the connection to the SQL server.
    /// </summary>
    /// <returns>An <see cref="OperationResult{T}"/> containing a boolean indicating if the connection was successful.</returns>
    public OperationResult<bool> TestSqlConnection();

    /// <summary>
    /// Synchronously executes a SQL statement and returns the number of rows affected.
    /// </summary>
    /// <param name="sqlStatement">The SQL text or stored procedure name to execute.</param>
    /// <param name="commandType">Specifies how the <paramref name="sqlStatement"/> is interpreted.</param>
    /// <param name="configureParameters">An optional action to populate the <see cref="SqlParameterCollection"/>.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the number of rows affected.</returns>
    public OperationResult<int> RunNonQueryCommand(string sqlStatement, CommandType commandType, Action<SqlParameterCollection>? configureParameters = null);

    /// <summary>
    /// Synchronously executes a SQL statement, providing direct access to the <see cref="SqlCommand"/> via a callback.
    /// </summary>
    /// <param name="sqlStatement">The SQL text or stored procedure name to execute.</param>
    /// <param name="commandType">Specifies how the <paramref name="sqlStatement"/> is interpreted.</param>
    /// <param name="processCommand">An asynchronous function (invoked synchronously) to process the command.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating success or failure.</returns>
    public NullOperationResult RunNonQueryCommand(string sqlStatement, CommandType commandType, Func<SqlCommand, Task> processCommand);

    /// <summary>
    /// Synchronously executes a query and provides a <see cref="SqlDataReader"/> to a callback for processing.
    /// </summary>
    /// <param name="sqlStatement">The SQL query to execute.</param>
    /// <param name="commandType">Specifies how the <paramref name="sqlStatement"/> is interpreted.</param>
    /// <param name="processReader">An asynchronous function (invoked synchronously) to handle the data reading logic.</param>
    /// <param name="configureParameters">An optional action to populate the <see cref="SqlParameterCollection"/>.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating success or failure.</returns>
    public NullOperationResult RunDataReader(string sqlStatement, CommandType commandType, Func<SqlDataReader, Task> processReader, Action<SqlParameterCollection>? configureParameters = null);
    #endregion

    #region Credentials
    /// <summary>
    /// Injects a secret store implementation to be used for credential retrieval.
    /// </summary>
    /// <param name="secretStore">The implementation of <see cref="ISecretStore"/> to use.</param>
    public void SetSecretStore(ISecretStore secretStore);

    /// <summary>
    /// Manually sets the connection credentials for the client.
    /// </summary>
    /// <param name="server">The SQL Server address.</param>
    /// <param name="database">The target database name.</param>
    /// <param name="username">The login username.</param>
    /// <param name="password">The login password.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating if the credentials were successfully applied.</returns>
    public NullOperationResult SetCredentials(string server, string database, string username, string password);

    /// <summary>
    /// Deletes a specific credential from the store based on the provided key.
    /// </summary>
    /// <param name="key">The key identifying the credential to remove.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating the result of the deletion.</returns>
    public NullOperationResult DeleteCredential(string key);

    /// <summary>
    /// Removes all stored credentials associated with this client.
    /// </summary>
    /// <returns>A <see cref="NullOperationResult"/> indicating the result of the deletion.</returns>
    public NullOperationResult DeleteAllCredentials();
    #endregion

    /// <summary>
    /// Outputs the current runtime settings to the configured output or log.
    /// </summary>
    /// <param name="calledFromManager">Indicates if this call was initiated by a management component.</param>
    public void OutputRuntimeSettings(bool calledFromManager = false);
}
