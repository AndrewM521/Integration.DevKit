/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.Core;
using Integration.DevKit.CredentialMgmt.Contracts;

namespace Integration.DevKit.CredentialMgmt;

/// <summary>
/// Utility methods to simplify data extraction
/// </summary>
public static class CredentialMgmtUtils
{
    /// <summary>
    /// Retrieves a required option value, converting it to <typeparamref name="T"/> if necessary.
    /// </summary>
    /// <remarks>
    /// Intended for use inside a <see cref="Service_CredentialMgmt.RegisterProvider"/> registration delegate,
    /// so a missing or mistyped option fails with a clear, provider-named message instead of a raw
    /// <see cref="KeyNotFoundException"/> or <see cref="InvalidCastException"/>.
    /// </remarks>
    /// <typeparam name="T">The expected type of the option value.</typeparam>
    /// <param name="options">The options dictionary, as handed to a provider's registration delegate.</param>
    /// <param name="key">The option's key.</param>
    /// <param name="providerName">The provider name, used only to make the exception message actionable.</param>
    /// <returns>The option value, converted to <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <paramref name="key"/> is missing/null, or its value cannot be converted to <typeparamref name="T"/>.
    /// </exception>
    public static T GetRequiredOption<T>(this Dictionary<string, object> options, string key, string providerName)
    {
        if (!options.TryGetValue(key, out var value) || value is null)
        {
            throw new InvalidOperationException(
                $"CredentialManagement provider '{providerName}' requires an Options[\"{key}\"] value, but none was configured.");
        }

        if (value is T typed)
        {
            return typed;
        }

        try
        {
            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"CredentialManagement provider '{providerName}' Options[\"{key}\"] could not be converted to {typeof(T).Name}.", ex);
        }
    }

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
