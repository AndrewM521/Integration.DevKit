using Integration.DevKit.ThreadLocks;

namespace Integration.DevKit.ThreadSafeItems.Tests;

public class ThreadSafeFileIOTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "devkit-tsio-" + Guid.NewGuid().ToString("N"));

    public ThreadSafeFileIOTests()
    {
        Directory.CreateDirectory(Path.Combine(_dir, "sub"));
    }

    public void Dispose()
    {
        Directory.Delete(_dir, true);
    }

    [Fact]
    public async Task ParallelAppends_ThroughDifferentPathSpellings_AllLand()
    {
        // Each spelling used to get its own lock, so writers collided on the same file
        var fileIO = new ThreadSafeFileIO(new ThreadLockManager());
        var spellings = new[]
        {
            Path.Combine(_dir, "out.txt"),
            Path.Combine(_dir, ".", "out.txt"),
            Path.Combine(_dir, "sub", "..", "out.txt")
        };

        var writes = Enumerable.Range(0, 30)
            .Select(i => Task.Run(() => fileIO.WriteToFileAsync(spellings[i % spellings.Length], $"line {i}", append: true)))
            .ToArray();
        var results = await Task.WhenAll(writes);

        Assert.All(results, r => Assert.True(r.MethodSuccess, r.Exception?.Message));
        Assert.Equal(30, File.ReadAllLines(spellings[0]).Length);
    }

    [Fact]
    public async Task FileWithoutExtension_CanBeReadBack()
    {
        // The read methods had no allowNoFileExtension, so files they could write couldn't be read
        var fileIO = new ThreadSafeFileIO(new ThreadLockManager());
        var path = Path.Combine(_dir, "LICENSE");

        Assert.True(fileIO.WriteToFile(path, "MIT", allowNoFileExtension: true).MethodSuccess);

        Assert.Equal("MIT", fileIO.ReadFileText(path, allowNoFileExtension: true).Result.Trim());
        Assert.Equal("MIT", (await fileIO.ReadFileTextAsync(path, allowNoFileExtension: true)).Result.Trim());
        Assert.Single(fileIO.ReadFileLines(path, allowNoFileExtension: true).Result);
        Assert.Single((await fileIO.ReadFileLinesAsync(path, allowNoFileExtension: true)).Result);
    }
}
