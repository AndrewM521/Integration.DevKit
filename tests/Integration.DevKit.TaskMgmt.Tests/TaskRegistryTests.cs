using Integration.DevKit.TaskMgmt.Contracts;

namespace Integration.DevKit.TaskMgmt.Tests;

public class TaskRegistryTests
{
    [Fact]
    public void Upsert_ThenTryGet_ReturnsStoredSnapshot()
    {
        var registry = new TaskRegistry();
        var snapshot = new ManagedTaskSnapshot("key1", new ManagedTaskSettings());

        var upsert = registry.Upsert(snapshot);
        Assert.True(upsert.MethodSuccess);

        var get = registry.TryGet("key1");
        Assert.True(get.MethodSuccess);
        Assert.Same(snapshot, get.Result);
    }

    [Fact]
    public void Upsert_ExistingKey_Overwrites()
    {
        var registry = new TaskRegistry();
        var first = new ManagedTaskSnapshot("key1", new ManagedTaskSettings());
        var second = new ManagedTaskSnapshot("key1", new ManagedTaskSettings());

        registry.Upsert(first);
        registry.Upsert(second);

        var get = registry.TryGet("key1");
        Assert.Same(second, get.Result);
    }

    [Fact]
    public void TryGet_MissingKey_SucceedsWithNullResult()
    {
        var registry = new TaskRegistry();

        var get = registry.TryGet("missing");

        Assert.True(get.MethodSuccess);
        Assert.Null(get.Result);
    }

    [Fact]
    public void Remove_ExistingKey_RemovesSnapshot()
    {
        var registry = new TaskRegistry();
        registry.Upsert(new ManagedTaskSnapshot("key1", new ManagedTaskSettings()));

        var remove = registry.Remove("key1");

        Assert.True(remove.MethodSuccess);
        Assert.Null(registry.TryGet("key1").Result);
    }

    [Fact]
    public void Remove_MissingKey_StillSucceeds()
    {
        var registry = new TaskRegistry();

        var remove = registry.Remove("missing");

        Assert.True(remove.MethodSuccess);
    }

    [Fact]
    public void Snapshots_ExposesAllUpsertedEntries()
    {
        var registry = new TaskRegistry();
        registry.Upsert(new ManagedTaskSnapshot("key1", new ManagedTaskSettings()));
        registry.Upsert(new ManagedTaskSnapshot("key2", new ManagedTaskSettings()));

        Assert.Equal(2, registry.Snapshots.Count);
        Assert.Contains("key1", registry.Snapshots.Keys);
        Assert.Contains("key2", registry.Snapshots.Keys);
    }
}
