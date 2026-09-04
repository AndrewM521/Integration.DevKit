/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

namespace Integration.DevKit.CredentialMgmt.Settings;

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
    /// Gets or sets which backend to register — the built-in "File" provider, or any custom provider name
    /// registered via <c>Service_CredentialMgmt.RegisterProvider</c>.
    /// </summary>
    public string Provider { get; set; } = "File";

    /// <summary>
    /// Flat, provider-specific settings for whichever provider <see cref="Provider"/> selects, bound from the
    /// <c>"Integration.DevKit:CredentialManagement:Options"</c> configuration section. Each provider defines
    /// and documents its own expected keys — this library places no constraints on what a provider stores here,
    /// which is what lets a custom provider be added without any change to this settings class.
    /// </summary>
    /// <remarks>Keys are matched case-insensitively.</remarks>
    public Dictionary<string, object> Options { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets whether this module logs through the logger factory supplied at registration.
    /// Defaults to <see langword="true"/>. Can be flipped at runtime via the resolved store's own
    /// <c>EnableLogging</c> property to silence/resume this module's logging without removing the
    /// app's logger.
    /// </summary>
    public bool EnableLogging { get; set; } = true;
}
