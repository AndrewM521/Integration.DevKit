using AndrewM5.DevKit.Core.Results;

namespace AndrewM5.DevKit.CredentialManagement.Contracts.Interfaces;

/// <summary>
/// Defines a contract for a secure storage mechanism used to manage sensitive keys and secrets.
/// </summary>
public interface ISecretStore
{
    /// <summary>
    /// Gets the unique identifier or display name of the secret store implementation.
    /// </summary>
    public string StoreName { get; }

    /// <summary>
    /// Creates or updates a specific key-value pair within the designated secret file.
    /// </summary>
    /// <param name="fileName">The name or identifier of the secret container.</param>
    /// <param name="key">The unique identifier for the secret value.</param>
    /// <param name="value">The sensitive data to be stored.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating whether the store operation was successful.</returns>
    public NullOperationResult SetKey(string fileName, string key, string value);

    /// <summary>
    /// Retrieves a stored value associated with a specific key from the designated secret file.
    /// </summary>
    /// <param name="fileName">The name or identifier of the secret container.</param>
    /// <param name="key">The unique identifier for the secret value to retrieve.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the secret string if found.</returns>
    public OperationResult<string> GetKey(string fileName, string key);

    /// <summary>
    /// Removes a specific key and its associated value from the designated secret file.
    /// </summary>
    /// <param name="fileName">The name or identifier of the secret container.</param>
    /// <param name="key">The unique identifier for the secret value to delete.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating whether the deletion was successful.</returns>
    public NullOperationResult DeleteKey(string fileName, string key);

    /// <summary>
    /// Deletes an entire secret container and all keys held within it.
    /// </summary>
    /// <param name="fileName">The name or identifier of the secret container to remove.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating whether the secret file was successfully deleted.</returns>
    public NullOperationResult DeleteSecret(string fileName);
}
