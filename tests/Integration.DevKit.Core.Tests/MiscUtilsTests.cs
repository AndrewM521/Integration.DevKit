namespace Integration.DevKit.Core.Tests;

public class MiscUtilsTests
{
    [Fact]
    public void ConvertUnixToCentralTime_KnownTimestamp_ConvertsCorrectly()
    {
        // 2024-01-01 00:00:00 UTC -> 2023-12-31 18:00:00 CST (UTC-6, standard time, no DST in January)
        long unixSeconds = 1704067200;

        var result = Integration.DevKit.Core.MiscUtils.ConvertUnixToCentralTime(unixSeconds);

        Assert.Equal(new DateTime(2023, 12, 31, 18, 0, 0), result);
    }

    [Fact]
    public void ConvertUnixToCentralTime_Epoch_DoesNotThrow()
    {
        var result = Integration.DevKit.Core.MiscUtils.ConvertUnixToCentralTime(0);

        Assert.Equal(1969, result.Year);
    }
}
