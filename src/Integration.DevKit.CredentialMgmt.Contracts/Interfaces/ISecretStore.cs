/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.Core;

namespace Integration.DevKit.CredentialMgmt.Contracts;

/// <summary>
/// Defines a contract for a secure storage mechanism used to manage sensitive keys and secrets.
/// </summary>
/// <remarks>
/// Implementations of this interface should ensure that data is encrypted at rest and 
/// that access to the underlying storage medium is restricted.
/// </remarks>
public interface ISecretStore
{
    /// <summary>
    /// Gets the unique identifier or display name of the secret store implementation.
    /// </summary>
    /// <value>A string representing the type or instance of the store (e.g., "FileSecretStore").</value>
    public string StoreName { get; }

    /// <summary>
    /// Creates or updates a specific key-value pair within the designated secret container.
    /// </summary>
    /// <param name="fileName">The name or identifier of the secret container (e.g., a filename or database table).</param>
    /// <param name="key">The unique identifier for the secret value within the container.</param>
    /// <param name="value">The sensitive data to be stored.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating success or containing exception details on failure.</returns>
    public NullOperationResult SetKey(string fileName, string key, string value);

    /// <summary>
    /// Retrieves a stored value associated with a specific key from the designated secret container.
    /// </summary>
    /// <param name="fileName">The name or identifier of the secret container.</param>
    /// <param name="key">The unique identifier for the secret value to retrieve.</param>
    /// <returns>
    /// An <see cref="OperationResult{T}"/> containing the decrypted secret string. 
    /// If the key is not found, the result should indicate failure with a <see cref="KeyNotFoundException"/>.
    /// </returns>
    public OperationResult<string> GetKey(string fileName, string key);

    /// <summary>
    /// Removes a specific key and its associated value from the designated secret container.
    /// </summary>
    /// <param name="fileName">The name or identifier of the secret container.</param>
    /// <param name="key">The unique identifier for the secret value to delete.</param>
    /// <returns>
    /// A <see cref="NullOperationResult"/>. This operation should be idempotent; 
    /// returning success even if the key or container does not exist.
    /// </returns>
    public NullOperationResult DeleteKey(string fileName, string key);

    /// <summary>
    /// Deletes an entire secret container and all keys held within it.
    /// </summary>
    /// <param name="fileName">The name or identifier of the secret container to remove.</param>
    /// <returns>
    /// A <see cref="NullOperationResult"/>. This operation should be idempotent; 
    /// returning success even if the container does not exist.
    /// </returns>
    public NullOperationResult DeleteSecret(string fileName);
}
