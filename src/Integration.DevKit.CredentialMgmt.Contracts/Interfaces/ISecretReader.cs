/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.Core;

namespace Integration.DevKit.CredentialMgmt.Contracts;

/// <summary>
/// Defines a read-only contract for retrieving secrets from a source.
/// </summary>
/// <remarks>
/// This is the minimal contract a secret source needs to implement to participate in credential
/// resolution — e.g. environment variables, <see cref="Microsoft.Extensions.Configuration.IConfiguration"/>
/// (including ASP.NET Core User Secrets), or a read-only view onto a cloud vault. Sources that also
/// support writing implement <see cref="ISecretStore"/> instead.
/// </remarks>
public interface ISecretReader
{
    /// <summary>
    /// Gets the unique identifier or display name of the secret source.
    /// </summary>
    /// <value>A string representing the type or instance of the source (e.g., "FileSecretStore").</value>
    public string StoreName { get; }

    /// <summary>
    /// Retrieves a stored value associated with a specific key from the designated secret container.
    /// </summary>
    /// <param name="fileName">The name or identifier of the secret container.</param>
    /// <param name="key">The unique identifier for the secret value to retrieve.</param>
    /// <returns>
    /// An <see cref="OperationResult{T}"/> containing the secret string.
    /// If the key is not found, the result should indicate failure with a <see cref="KeyNotFoundException"/>.
    /// </returns>
    public OperationResult<string> GetKey(string fileName, string key);
}
