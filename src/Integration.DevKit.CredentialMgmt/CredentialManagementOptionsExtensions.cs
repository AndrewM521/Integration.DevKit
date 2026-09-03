/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

namespace Integration.DevKit.CredentialMgmt;

/// <summary>
/// Extension methods for reading typed values out of a <c>CredentialManagementSettings.Options</c> dictionary.
/// </summary>
public static class CredentialManagementOptionsExtensions
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
}
