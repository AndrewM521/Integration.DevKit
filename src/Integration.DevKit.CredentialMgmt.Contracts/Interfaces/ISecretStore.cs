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
/// Extends <see cref="ISecretReader"/> with write operations. Implementations of this interface
/// should ensure that data is encrypted at rest and that access to the underlying storage medium
/// is restricted. Sources that only need to be read from (env vars, <c>IConfiguration</c>, a
/// read-only vault view) only need to implement <see cref="ISecretReader"/>.
/// </remarks>
public interface ISecretStore : ISecretReader
{
    /// <summary>
    /// Creates or updates a specific key-value pair within the designated secret container.
    /// </summary>
    /// <param name="fileName">The name or identifier of the secret container (e.g., a filename or database table).</param>
    /// <param name="key">The unique identifier for the secret value within the container.</param>
    /// <param name="value">The sensitive data to be stored.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating success or containing exception details on failure.</returns>
    public NullOperationResult SetKey(string fileName, string key, string value);

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
