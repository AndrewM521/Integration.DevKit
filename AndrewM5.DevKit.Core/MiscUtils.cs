using System.Runtime.InteropServices;

namespace AndrewM5.DevKit.Core;

public static class MiscUtils
{
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
