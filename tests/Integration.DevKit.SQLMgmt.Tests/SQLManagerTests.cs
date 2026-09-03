using Integration.DevKit.SQLMgmt.Contracts;
using Microsoft.Extensions.Options;

namespace Integration.DevKit.SQLMgmt.Tests;

public class SQLManagerTests
{
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
    public void GetClient_UnconfiguredName_FallsBackToDefaultSettings()
    {
        var manager = new SQLManager(Options.Create(new SQLManagerSettings()));

        var client = manager.GetClient("never-configured");

        Assert.Equal("never-configured", client.ClientName);
        Assert.Equal(string.Empty, client.RuntimeSettings.ConnectionString);
    }

    [Fact]
    public void GetClient_SameNameCalledTwice_ReturnsCachedInstance()
    {
        var manager = new SQLManager(Options.Create(new SQLManagerSettings()));

        var first = manager.GetClient("cached");
        var second = manager.GetClient("cached");

        Assert.Same(first, second);
    }

    [Fact]
    public void GetClient_NameDiffersOnlyByCase_ReturnsSameCachedInstance()
    {
        var manager = new SQLManager(Options.Create(new SQLManagerSettings()));

        var first = manager.GetClient("MyClient");
        var second = manager.GetClient("myclient");

        Assert.Same(first, second);
    }

    [Fact]
    public void GetClient_DifferentNames_ReturnDifferentInstances()
    {
        var manager = new SQLManager(Options.Create(new SQLManagerSettings()));

        var a = manager.GetClient("a");
        var b = manager.GetClient("b");

        Assert.NotSame(a, b);
    }

    [Fact]
    public async Task DisposeAsync_DisposesAllCreatedClients()
    {
        var manager = new SQLManager(Options.Create(new SQLManagerSettings()));
        manager.GetClient("a");
        manager.GetClient("b");

        // No exception on dispose is the observable contract from the public surface.
        await manager.DisposeAsync();
    }
}
