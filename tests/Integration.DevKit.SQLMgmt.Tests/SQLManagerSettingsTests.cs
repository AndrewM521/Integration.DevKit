using Integration.DevKit.SQLMgmt.Settings;

namespace Integration.DevKit.SQLMgmt.Tests;

public class SQLManagerSettingsTests
{
    [Fact]
    public void Clone_ProducesIndependentClientsDictionary()
    {
        var original = new SQLManagerSettings();
        original.Clients["a"] = new SQLClientSettings { ConnectionString = "Server=a;" };

        var clone = original.Clone();
        clone.Clients["b"] = new SQLClientSettings { ConnectionString = "Server=b;" };

        Assert.Single(original.Clients);
        Assert.Equal(2, clone.Clients.Count);
    }

    [Fact]
    public void Clone_CopiesExistingClientEntries()
    {
        var original = new SQLManagerSettings();
        original.Clients["a"] = new SQLClientSettings { ConnectionString = "Server=a;" };

        var clone = original.Clone();

        Assert.Equal("Server=a;", clone.Clients["a"].ConnectionString);
    }
}

public class SQLClientSettingsTests
{
    [Fact]
    public void Defaults_AreEmptyConnectionStringAndSingleConnectionDisabled()
    {
        var settings = new SQLClientSettings();

        Assert.Equal(string.Empty, settings.ConnectionString);
        Assert.False(settings.UseSingleConnection);
    }
}
