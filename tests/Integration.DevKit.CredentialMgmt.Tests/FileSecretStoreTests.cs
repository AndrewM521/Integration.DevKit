using Integration.DevKit.ThreadLocks;
using Integration.DevKit.ThreadLocks.Contracts;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.DependencyInjection;

namespace Integration.DevKit.CredentialMgmt.Tests;

public class FileSecretStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly IDataProtectionProvider _provider;
    private readonly IThreadLockManager _threadLockManager = new ThreadLockManager();

    public FileSecretStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "devkit-credmgmt-tests-" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDir);

        // Key ring rooted at a temp folder (via the same AddDataProtection/PersistKeysToFileSystem
        // wiring Service_CredentialMgmt's "File" provider uses) — no real Windows user-profile dependency.
        var services = new ServiceCollection();
        services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(_tempDir, "keys")));
        _provider = services.BuildServiceProvider().GetRequiredService<IDataProtectionProvider>();
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
        {
            Directory.Delete(_tempDir, recursive: true);
        }
    }

    private FileSecretStore CreateStore(string appName = "TestApp") =>
        new(_provider, appName, Path.Combine(_tempDir, "secrets"), _threadLockManager);

    [Fact]
    public void SetKey_ThenGetKey_RoundTrips()
    {
        var store = CreateStore();

        var set = store.SetKey("container", "username", "alice");
        Assert.True(set.MethodSuccess);

        var get = store.GetKey("container", "username");
        Assert.True(get.MethodSuccess);
        Assert.Equal("alice", get.Result);
    }

    [Fact]
    public void GetKey_MissingKey_FailsWithKeyNotFoundException()
    {
        var store = CreateStore();
        store.SetKey("container", "username", "alice");

        var get = store.GetKey("container", "password");

        Assert.False(get.MethodSuccess);
        Assert.IsType<KeyNotFoundException>(get.Exception);
    }

    [Fact]
    public void GetKey_MissingContainer_FailsWithKeyNotFoundException()
    {
        var store = CreateStore();

        var get = store.GetKey("never-created", "username");

        Assert.False(get.MethodSuccess);
        Assert.IsType<KeyNotFoundException>(get.Exception);
    }

    [Fact]
    public void SetKey_MultipleKeysInSameContainer_AllPersist()
    {
        var store = CreateStore();

        store.SetKey("container", "username", "alice");
        store.SetKey("container", "password", "hunter2");

        Assert.Equal("alice", store.GetKey("container", "username").Result);
        Assert.Equal("hunter2", store.GetKey("container", "password").Result);
    }

    [Fact]
    public void SetKey_OverwritesExistingValue()
    {
        var store = CreateStore();
        store.SetKey("container", "username", "alice");

        store.SetKey("container", "username", "bob");

        Assert.Equal("bob", store.GetKey("container", "username").Result);
    }

    [Fact]
    public void DeleteKey_RemovesOnlyThatKey()
    {
        var store = CreateStore();
        store.SetKey("container", "username", "alice");
        store.SetKey("container", "password", "hunter2");

        var delete = store.DeleteKey("container", "username");

        Assert.True(delete.MethodSuccess);
        Assert.False(store.GetKey("container", "username").MethodSuccess);
        Assert.Equal("hunter2", store.GetKey("container", "password").Result);
    }

    [Fact]
    public void DeleteKey_MissingContainer_IsIdempotentSuccess()
    {
        var store = CreateStore();

        var delete = store.DeleteKey("never-created", "username");

        Assert.True(delete.MethodSuccess);
    }

    [Fact]
    public void DeleteKey_MissingKeyInExistingContainer_IsIdempotentSuccess()
    {
        var store = CreateStore();
        store.SetKey("container", "username", "alice");

        var delete = store.DeleteKey("container", "password");

        Assert.True(delete.MethodSuccess);
    }

    [Fact]
    public void DeleteSecret_RemovesEntireContainer()
    {
        var store = CreateStore();
        store.SetKey("container", "username", "alice");
        store.SetKey("container", "password", "hunter2");

        var delete = store.DeleteSecret("container");

        Assert.True(delete.MethodSuccess);
        Assert.False(store.GetKey("container", "username").MethodSuccess);
    }

    [Fact]
    public void DeleteSecret_MissingContainer_IsIdempotentSuccess()
    {
        var store = CreateStore();

        var delete = store.DeleteSecret("never-created");

        Assert.True(delete.MethodSuccess);
    }

    [Fact]
    public void SecretsAreStoredEncryptedOnDisk()
    {
        var store = CreateStore();
        store.SetKey("container", "username", "super-secret-plaintext-marker");

        var files = Directory.GetFiles(Path.Combine(_tempDir, "secrets"), "*.secret");
        Assert.Single(files);

        var rawContent = File.ReadAllText(files[0]);
        Assert.DoesNotContain("super-secret-plaintext-marker", rawContent);
    }

    [Fact]
    public void DifferentApplicationNames_AreIsolatedFromEachOther()
    {
        var storeA = CreateStore("AppA");
        var storeB = CreateStore("AppB");

        storeA.SetKey("container", "username", "alice");

        var getFromB = storeB.GetKey("container", "username");
        Assert.False(getFromB.MethodSuccess);
    }

    [Fact]
    public void Constructor_NullThreadLockManager_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            new FileSecretStore(_provider, "TestApp", Path.Combine(_tempDir, "secrets"), null!));
    }

    [Fact]
    public async Task SetKey_ConcurrentCallsOnSameContainer_NoLostUpdates()
    {
        var store = CreateStore();
        const int concurrentWrites = 20;

        var tasks = Enumerable.Range(0, concurrentWrites)
            .Select(i => Task.Run(() => store.SetKey("container", $"key{i}", $"value{i}")))
            .ToArray();

        await Task.WhenAll(tasks);

        for (int i = 0; i < concurrentWrites; i++)
        {
            var get = store.GetKey("container", $"key{i}");
            Assert.True(get.MethodSuccess, $"key{i} was lost to a concurrent-write race.");
            Assert.Equal($"value{i}", get.Result);
        }
    }

    [Fact]
    public async Task SetKey_ConcurrentCallsOnDifferentContainers_AllPersistCorrectly()
    {
        var store = CreateStore();
        const int concurrentContainers = 10;

        var tasks = Enumerable.Range(0, concurrentContainers)
            .Select(i => Task.Run(() => store.SetKey($"container{i}", "value", $"v{i}")))
            .ToArray();

        await Task.WhenAll(tasks);

        for (int i = 0; i < concurrentContainers; i++)
        {
            var get = store.GetKey($"container{i}", "value");
            Assert.True(get.MethodSuccess);
            Assert.Equal($"v{i}", get.Result);
        }
    }
}
