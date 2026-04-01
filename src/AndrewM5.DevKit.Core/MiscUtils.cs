using System.Runtime.InteropServices;

namespace AndrewM5.DevKit.Core;

/// <summary>
/// Provides miscellaneous utility methods for common data conversions and system operations.
/// </summary>
public static class MiscUtils
{
    /// <summary>
    /// Converts a Unix timestamp (seconds) to a <see cref="DateTime"/> in Central Standard Time (CST/CDT).
    /// </summary>
    /// <remarks>
    /// This method is cross-platform aware. It uses "Central Standard Time" for Windows-based systems 
    /// and the IANA ID "America/Chicago" for Linux, macOS, and containerized environments.
    /// </remarks>
    /// <param name="unixSeconds">The number of seconds that have elapsed since the Unix epoch (January 1, 1970).</param>
    /// <returns>A <see cref="DateTime"/> object representing the equivalent time in the Central Time Zone.</returns>
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
