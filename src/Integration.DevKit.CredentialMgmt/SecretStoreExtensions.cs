/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.Core;
using Integration.DevKit.CredentialMgmt.Contracts;

namespace Integration.DevKit.CredentialMgmt;

/// <summary>
/// Extension methods for <see cref="ISecretStore"/>.
/// </summary>
public static class SecretStoreExtensions
{
    /// <summary>
    /// Reads a secret from <paramref name="source"/> and writes it into <paramref name="target"/>.
    /// </summary>
    /// <remarks>
    /// Gives every backend the same one-time "seed the encrypted store" code path, regardless of
    /// where the plaintext originally lives (env var, User Secrets, CI secret, etc.) — the plaintext
    /// moment happens exactly once, at import time, through this single method.
    /// </remarks>
    /// <param name="target">The store to write the secret into.</param>
    /// <param name="source">The reader to read the secret from.</param>
    /// <param name="fileName">The name or identifier of the secret container.</param>
    /// <param name="key">The unique identifier for the secret value.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating success or containing exception details on failure.</returns>
    public static NullOperationResult ImportFrom(this ISecretStore target, ISecretReader source, string fileName, string key)
    {
        var result = new NullOperationResult();

        try
        {
            if (target == null)
            {
                throw new ArgumentNullException(nameof(target));
            }

            if (source == null)
            {
                throw new ArgumentNullException(nameof(source));
            }

            var readResult = source.GetKey(fileName, key);
            if (!readResult.MethodSuccess)
            {
                throw readResult.Exception;
            }

            var setResult = target.SetKey(fileName, key, readResult.Result);
            if (!setResult.MethodSuccess)
            {
                throw setResult.Exception;
            }

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }
}
