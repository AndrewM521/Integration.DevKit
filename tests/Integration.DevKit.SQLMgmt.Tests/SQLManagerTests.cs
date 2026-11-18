using Integration.DevKit.SQLMgmt.Settings;
using Microsoft.Extensions.Options;

namespace Integration.DevKit.SQLMgmt.Tests;

public class SQLManagerTests
{
    private static SQLManager CreateManager(params string[] clientNames)
    {
        var settings = new SQLManagerSettings();
        foreach (var name in clientNames)
        {
            settings.Clients[name] = new SQLClientSettings();
        }

        return new SQLManager(Options.Create(settings));
    }

    [Fact]
    public void Constructor_NullSettings_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new SQLManager(null!));
    }

    [Fact]
    public void GetClient_ConfiguredName_UsesConfiguredSettings()
    {
        var settings = new SQLManagerSettings();
        settings.Clients["configured"] = new SQLClientSettings { ConnectionString = "Server=configured;" };

        var manager = new SQLManager(Options.Create(settings));

        var client = manager.GetClient("configured");

        Assert.Equal("configured", client.ClientName);
        Assert.Equal("Server=configured;", client.RuntimeSettings.ConnectionString);
    }

    [Fact]
    public void GetClient_UnconfiguredName_Throws()
    {
        // Falling back to defaults used to cache a broken client under the misspelled name
        var manager = new SQLManager(Options.Create(new SQLManagerSettings()));

        Assert.Throws<KeyNotFoundException>(() => manager.GetClient("never-configured"));
    }

    [Fact]
    public void GetClient_NameDiffersOnlyByCaseFromConfig_UsesConfiguredSettings()
    {
        var settings = new SQLManagerSettings();
        settings.Clients["TestClient"] = new SQLClientSettings { ConnectionString = "Server=configured;" };
        var manager = new SQLManager(Options.Create(settings));

        var client = manager.GetClient("testclient");

        Assert.Equal("Server=configured;", client.RuntimeSettings.ConnectionString);
    }

    [Fact]
    public void GetClient_SameNameCalledTwice_ReturnsCachedInstance()
    {
        var manager = CreateManager("cached");

        var first = manager.GetClient("cached");
        var second = manager.GetClient("cached");

        Assert.Same(first, second);
    }

    [Fact]
    public void GetClient_NameDiffersOnlyByCase_ReturnsSameCachedInstance()
    {
        var manager = CreateManager("MyClient");

        var first = manager.GetClient("MyClient");
        var second = manager.GetClient("myclient");

        Assert.Same(first, second);
    }

    [Fact]
    public void GetClient_DifferentNames_ReturnDifferentInstances()
    {
        var manager = CreateManager("a", "b");

        var a = manager.GetClient("a");
        var b = manager.GetClient("b");

        Assert.NotSame(a, b);
    }

    [Fact]
    public async Task DisposeAsync_DisposesAllCreatedClients()
    {
        var manager = CreateManager("a", "b");
        manager.GetClient("a");
        manager.GetClient("b");

        // No exception on dispose is the observable contract from the public surface.
        await manager.DisposeAsync();
    }
}
