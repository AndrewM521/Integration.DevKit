/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.Core;
using Integration.DevKit.CredentialMgmt.Contracts;

namespace Integration.DevKit.CredentialMgmt.Implementations;

/// <summary>
/// An <see cref="ISecretReader"/> that tries a prioritized list of readers in order and returns
/// the first successful result.
/// </summary>
/// <remarks>
/// This mirrors how <see cref="Microsoft.Extensions.Configuration.IConfiguration"/> itself layers
/// providers. A consuming project decides its own source priority (e.g. environment variable first,
/// then the encrypted <see cref="FileSecretStore"/>, then a cloud vault) by choosing the order the
/// readers are supplied in — DevKit only supplies the composition logic.
/// </remarks>
public class CompositeSecretReader : ISecretReader
{
    private readonly IReadOnlyList<ISecretReader> _readers;

    /// <inheritdoc />
    public string StoreName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CompositeSecretReader"/> class.
    /// </summary>
    /// <param name="readers">The readers to try, in priority order. The first to succeed wins.</param>
    /// <param name="storeName">
    /// The display name for this reader. Defaults to "CompositeSecretReader".
    /// </param>
    public CompositeSecretReader(IReadOnlyList<ISecretReader> readers, string storeName = "CompositeSecretReader")
    {
        _readers = readers ?? throw new ArgumentNullException(nameof(readers));
        StoreName = storeName;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Returns the first successful <see cref="ISecretReader.GetKey"/> result across the configured
    /// readers, in order. If every reader fails, the result fails with a <see cref="KeyNotFoundException"/>
    /// summarizing that none of the sources had the key.
    /// </remarks>
    public OperationResult<string> GetKey(string fileName, string key)
    {
        var result = new OperationResult<string>();

        foreach (var reader in _readers)
        {
            var readerResult = reader.GetKey(fileName, key);
            if (readerResult.MethodSuccess)
            {
                return readerResult;
            }
        }

        return result.SetMethodFailure(
            new KeyNotFoundException($"Secret '{key}' not found in container '{fileName}' in any of {_readers.Count} configured reader(s)"));
    }
}
