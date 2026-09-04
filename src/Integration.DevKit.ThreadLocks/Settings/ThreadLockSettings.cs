/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

namespace Integration.DevKit.ThreadLocks.Settings;

/// <summary>
/// Configuration-bound settings for the Thread Locks module.
/// </summary>
/// <remarks>
/// Bound from the <c>Integration.DevKit:ThreadLocks</c> configuration section, matching the
/// binding convention used by the other DevKit modules (e.g. <c>Integration.DevKit:SQLManagement</c>).
/// </remarks>
public class ThreadLockSettings
{
    /// <summary>
    /// Gets or sets whether this module logs through the logger factory supplied at registration.
    /// Defaults to <see langword="true"/>. Can be flipped at runtime via the manager's
    /// <c>RuntimeSettings</c> to silence/resume this module's logging without removing the app's logger.
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Creates a new instance of <see cref="ThreadLockSettings"/> with the same property values.
    /// </summary>
    public ThreadLockSettings Clone()
    {
        return new ThreadLockSettings
        {
            EnableLogging = EnableLogging
        };
    }
}
