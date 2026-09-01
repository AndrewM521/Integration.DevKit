/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

namespace Integration.DevKit.CredentialMgmt.Contracts;

/// <summary>
/// Configuration-bound settings for selecting and configuring the credential management backend.
/// </summary>
/// <remarks>
/// Bound from the <c>Integration.DevKit:CredentialManagement</c> configuration section, matching the
/// binding convention used by the other DevKit modules (e.g. <c>Integration.DevKit:SQLManagement</c>).
/// </remarks>
public class CredentialManagementSettings
{
    /// <summary>
    /// Gets or sets which backend to register. Currently only "File" is supported.
    /// </summary>
    public string Provider { get; set; } = "File";

    /// <summary>
    /// Gets or sets the settings for the file-based backend, used when <see cref="Provider"/> is "File".
    /// </summary>
    public FileSecretStoreSettings File { get; set; } = new FileSecretStoreSettings();
}

/// <summary>
/// Configuration for the file-based secret store backend.
/// </summary>
public class FileSecretStoreSettings
{
    /// <summary>
    /// The unique name of the application. Used as the Data Protection purpose string and the
    /// identity of the store.
    /// </summary>
    public string ApplicationName { get; set; } = string.Empty;

    /// <summary>
    /// The directory path where encrypted secret files are stored.
    /// </summary>
    public string SecretsFolder { get; set; } = string.Empty;

    /// <summary>
    /// The directory path where the Data Protection XML master keys are persisted.
    /// </summary>
    public string KeysFolder { get; set; } = string.Empty;
}
