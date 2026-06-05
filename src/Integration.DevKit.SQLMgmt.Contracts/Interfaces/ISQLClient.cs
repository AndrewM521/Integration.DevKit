using Integration.DevKit.Core;
using Integration.DevKit.CredentialMgmt.Contracts;
using Microsoft.Data.SqlClient;
using System.Data;

namespace Integration.DevKit.SQLMgmt.Contracts;

/// <summary>
/// Defines a contract for a SQL Database Client capable of executing commands, 
/// managing credentials, and testing connectivity both synchronously and asynchronously.
/// </summary>
public interface ISQLClient : IDisposable
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

    #region Asynchronous Methods
    /// <summary>
    /// Asynchronously tests the connection to the SQL server using the current <see cref="RuntimeSettings"/>.
    /// </summary>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>An <see cref="OperationResult{T}"/> indicating success and containing a boolean result where <see langword="true"/> means the connection is valid.</returns>
    public Task<OperationResult<bool>> TestSqlConnectionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously executes a SQL statement and returns the number of rows affected.
    /// </summary>
    /// <param name="sqlStatement">The SQL text or stored procedure name to execute.</param>
    /// <param name="commandType">Specifies how the <paramref name="sqlStatement"/> is interpreted (e.g., Text or StoredProcedure).</param>
    /// <param name="configureParameters">An optional action to populate the <see cref="SqlParameterCollection"/> before execution.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the number of rows affected.</returns>
    public Task<OperationResult<int>> RunNonQueryCommandAsync(string sqlStatement, CommandType commandType, Action<SqlParameterCollection>? configureParameters = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously executes a SQL statement, providing direct access to the <see cref="SqlCommand"/> for custom processing.
    /// </summary>
    /// <param name="sqlStatement">The SQL text or stored procedure name to execute.</param>
    /// <param name="commandType">Specifies how the <paramref name="sqlStatement"/> is interpreted.</param>
    /// <param name="processCommand">An asynchronous function to process the <see cref="SqlCommand"/> before or during execution.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating the success or failure of the operation.</returns>
    public Task<NullOperationResult> RunNonQueryCommandAsync(string sqlStatement, CommandType commandType, Func<SqlCommand, Task> processCommand, CancellationToken cancellationToken = default);

    /// <summary>
    /// Asynchronously executes a query and provides a <see cref="SqlDataReader"/> to a callback for processing results.
    /// </summary>
    /// <param name="sqlStatement">The SQL query to execute.</param>
    /// <param name="commandType">Specifies how the <paramref name="sqlStatement"/> is interpreted.</param>
    /// <param name="processReader">An asynchronous function to handle the data reading logic using the provided <see cref="SqlDataReader"/>.</param>
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
    /// <param name="processCommand">An asynchronous function (invoked synchronously) to process the <see cref="SqlCommand"/>.</param>
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
    /// Injects a secret store implementation to be used for secure credential retrieval.
    /// </summary>
    /// <param name="secretStore">The implementation of <see cref="ISecretStore"/> to use.</param>
    public void SetSecretStore(ISecretStore secretStore);

    /// <summary>
    /// Manually sets the connection string for the client.
    /// </summary>
    /// <param name="connectionString">The full SQL connection string.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating if the connection string was successfully applied.</returns>
    public NullOperationResult SetSecretStoreCredentials(string connectionString);

    /// <summary>
    /// Deletes a specific credential from the store based on the provided key.
    /// </summary>
    /// <param name="key">The key identifying the credential to remove.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating the result of the deletion.</returns>
    public NullOperationResult DeleteCredential(string key);

    /// <summary>
    /// Removes all stored credentials associated with this client instance.
    /// </summary>
    /// <returns>A <see cref="NullOperationResult"/> indicating the result of the deletion.</returns>
    public NullOperationResult DeleteAllCredentials();
    #endregion

    /// <summary>
    /// Logging method to output current <see cref="SQLClientSettings"/> to the logs.
    /// </summary>
    /// <param name="calledFromManager">Indicates if the call originated from a management orchestrator.</param>
    public void LogRuntimeSettings(bool calledFromManager = false);
}
