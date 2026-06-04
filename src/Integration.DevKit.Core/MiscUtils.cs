/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using System.Runtime.InteropServices;

namespace Integration.DevKit.Core;

/// <summary>
/// Utility methods for miscellaneous data conversions and system operations.
/// </summary>
public static class MiscUtils
{
    /// <summary>
    /// Converts a Unix timestamp into a <see cref="DateTime"/> adjusted to the Central Time Zone (CST/CDT).
    /// </summary>
    /// <param name="unixSeconds">The number of seconds elapsed since the Unix epoch (January 1, 1970, UTC).</param>
    /// <returns>A <see cref="DateTime"/> representing the equivalent local time in the Central Time Zone.</returns>
    /// <remarks>
    /// <para>
    /// This method is <b>cross-platform aware</b>. It automatically resolves the correct Time Zone ID 
    /// based on the host operating system:
    /// <list type="bullet">
    /// <item><description><b>Windows:</b> Uses "Central Standard Time".</description></item>
    /// <item><description><b>Linux/macOS/Containers:</b> Uses the IANA ID "America/Chicago".</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// This ensures reliable time conversion across local development (Windows) and 
    /// cloud deployment (Linux-based Docker containers).
    /// </para>
    /// </remarks>
    public static DateTime ConvertUnixToCentralTime(long unixSeconds)
    {
        // 1. Convert Unix seconds to UTC DateTimeOffset
        DateTimeOffset utcTime = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);

        // 2. Identify the TimeZone 
        // Windows uses "Central Standard Time"
        // Linux/macOS/Containers typically use IANA IDs like "America/Chicago"
        string tzId = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
            ? "Central Standard Time"
            : "America/Chicago";

        TimeZoneInfo centralZone = TimeZoneInfo.FindSystemTimeZoneById(tzId);

        // 3. Convert to Central Time and return the DateTime
        return TimeZoneInfo.ConvertTime(utcTime, centralZone).DateTime;
    }
}
