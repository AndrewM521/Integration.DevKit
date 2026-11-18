namespace Integration.DevKit.Core.Tests;

public class FileUtilsPureTests
{
    [Theory]
    [InlineData(@"C:\temp\file.txt", true)]
    [InlineData(@"C:\temp\", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsStringValidFilePath_ValidatesStructure(string? path, bool expected)
    {
        var result = FileUtils.IsStringValidFilePath(path!);

        Assert.True(result.MethodSuccess);
        Assert.Equal(expected, result.Result);
    }

    [Fact]
    public void IsStringValidFilePath_NoExtension_InvalidByDefault_ValidWhenAllowed()
    {
        var disallowed = FileUtils.IsStringValidFilePath(@"C:\temp\Dockerfile");
        var allowed = FileUtils.IsStringValidFilePath(@"C:\temp\Dockerfile", allowNoFileExtension: true);

        Assert.False(disallowed.Result);
        Assert.True(allowed.Result);
    }

    [Fact]
    public void GetExtension_ReturnsLowercaseExtension()
    {
        var result = FileUtils.GetExtension(@"C:\temp\FILE.TXT");

        Assert.True(result.MethodSuccess);
        Assert.Equal(".txt", result.Result);
    }

    [Theory]
    [InlineData(".txt", true)]
    [InlineData(".json", false)]
    public void IsPathValidExtension_ComparesCaseInsensitively(string validExtension, bool expected)
    {
        var result = FileUtils.IsPathValidExtension(@"C:\temp\file.TXT", validExtension);

        Assert.True(result.MethodSuccess);
        Assert.Equal(expected, result.Result);
    }
}

public class DirectoryUtilsPureTests
{
    [Theory]
    [InlineData(@"C:\temp\", true)]
    [InlineData(@"C:\temp\subdir", true)]
    [InlineData(@"C:\temp\file.txt", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void IsStringValidDirectoryPath_ValidatesStructure(string? path, bool expected)
    {
        var result = DirectoryUtils.IsStringValidDirectoryPath(path!);

        Assert.True(result.MethodSuccess);
        Assert.Equal(expected, result.Result);
    }

    [Fact]
    public void GetSafeDirectoryName_ReplacesInvalidCharacters()
    {
        var result = DirectoryUtils.GetSafeDirectoryName("bad:name*here?");

        Assert.True(result.MethodSuccess);
        Assert.DoesNotContain(':', result.Result);
        Assert.DoesNotContain('*', result.Result);
        Assert.DoesNotContain('?', result.Result);
    }

    [Fact]
    public void GetSafeDirectoryName_CustomReplacementChar_IsUsed()
    {
        var result = DirectoryUtils.GetSafeDirectoryName("bad:name", '-');

        Assert.True(result.MethodSuccess);
        Assert.Equal("bad-name", result.Result);
    }

    [Fact]
    public void GetSafeDirectoryName_TrimsTrailingDotsAndSpaces()
    {
        var result = DirectoryUtils.GetSafeDirectoryName("name.. ");

        Assert.True(result.MethodSuccess);
        Assert.Equal("name", result.Result);
    }

    [Fact]
    public void GetSafeDirectoryName_WhitespaceInput_Fails()
    {
        var result = DirectoryUtils.GetSafeDirectoryName("   ");

        Assert.False(result.MethodSuccess);
    }
}

public class FileUtilsDiskTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "devkit-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, true);
        }
    }

    [Fact]
    public void MoveFile_ToANewNameWithoutExtension_RenamesTheFile()
    {
        // The extensionless name used to be read as a folder, so the move failed with DirectoryNotFoundException
        Directory.CreateDirectory(_dir);
        var source = Path.Combine(_dir, "license.tmp");
        File.WriteAllText(source, "MIT");
        var destination = Path.Combine(_dir, "LICENSE");

        var move = Integration.DevKit.Core.FileUtils.MoveFile(source, destination, allowNoFileExtension: true);

        Assert.True(move.MethodSuccess, move.Exception?.Message);
        Assert.Equal("MIT", File.ReadAllText(destination));
        Assert.False(File.Exists(source));
    }

    [Fact]
    public void WriteToFile_EmptyContent_TruncatesExistingFile()
    {
        var path = Path.Combine(_dir, "v1.2", "data.txt");
        Assert.True(Integration.DevKit.Core.FileUtils.WriteToFile(path, "old content").MethodSuccess);

        var write = Integration.DevKit.Core.FileUtils.WriteToFile(path, "");

        Assert.True(write.MethodSuccess);
        Assert.Equal("", File.ReadAllText(path));
    }

    [Fact]
    public async Task WriteToFileAsync_EmptyContent_TruncatesExistingFile()
    {
        var path = Path.Combine(_dir, "v1.2", "data.txt");
        Assert.True((await Integration.DevKit.Core.FileUtils.WriteToFileAsync(path, "old content")).MethodSuccess);

        var write = await Integration.DevKit.Core.FileUtils.WriteToFileAsync(path, "");

        Assert.True(write.MethodSuccess);
        Assert.Equal("", File.ReadAllText(path));
    }

    [Fact]
    public void MoveFile_IntoExistingFolderWithDotInName_MovesIntoFolder()
    {
        // "v1.2" looks like a file name; it used to be treated as the new file name instead of the target folder
        var source = Path.Combine(_dir, "data.txt");
        var folder = Path.Combine(_dir, "v1.2");
        Directory.CreateDirectory(folder);
        File.WriteAllText(source, "x");

        var move = Integration.DevKit.Core.FileUtils.MoveFile(source, folder);

        Assert.True(move.MethodSuccess);
        Assert.True(File.Exists(Path.Combine(folder, "data.txt")));
    }

    [Fact]
    public void ExistingFolderWithDotInName_IsListedAndCleanedAsAFolder()
    {
        // "v1.2" looks like a file name; GetFiles and DeleteFiles used to reject it as "Path must be a Directory"
        var folder = Path.Combine(_dir, "v1.2");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "a.log"), "x");

        var list = DirectoryUtils.GetFiles(folder);
        var delete = Integration.DevKit.Core.FileUtils.DeleteFiles(folder, "*.log");

        Assert.True(list.MethodSuccess);
        Assert.Single(list.Result);
        Assert.True(delete.MethodSuccess);
        Assert.Empty(Directory.GetFiles(folder));
    }

    [Fact]
    public void DeleteFiles_DeletesFilesWithoutExtension_AndDoesNotCreateMissingFolders()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, "LICENSE"), "x");
        var missing = Path.Combine(_dir, "missing");

        Assert.True(Integration.DevKit.Core.FileUtils.DeleteFiles(_dir, "*").MethodSuccess);
        Assert.False(File.Exists(Path.Combine(_dir, "LICENSE")));

        Assert.True(Integration.DevKit.Core.FileUtils.DeleteFiles(missing, "*").MethodSuccess);
        Assert.False(Directory.Exists(missing));
    }

    [Fact]
    public void WriteBytesToFile_EmptyContent_TruncatesExistingFile()
    {
        var path = Path.Combine(_dir, "data.bin");
        Assert.True(Integration.DevKit.Core.FileUtils.WriteBytesToFile(path, new byte[] { 1, 2, 3 }).MethodSuccess);

        Assert.True(Integration.DevKit.Core.FileUtils.WriteBytesToFile(path, Array.Empty<byte>()).MethodSuccess);

        Assert.Empty(File.ReadAllBytes(path));
    }

    [Fact]
    public async Task WriteToFile_EmptyArray_TruncatesTheFile()
    {
        // An empty array used to return early and leave the old content in place
        Directory.CreateDirectory(_dir);
        var path = Path.Combine(_dir, "rows.csv");
        File.WriteAllText(path, "old");

        Assert.True(Integration.DevKit.Core.FileUtils.WriteToFile(path, Array.Empty<string>()).MethodSuccess);
        Assert.Equal("", File.ReadAllText(path));

        File.WriteAllText(path, "old");
        Assert.True((await Integration.DevKit.Core.FileUtils.WriteToFileAsync(path, Array.Empty<string>())).MethodSuccess);
        Assert.Equal("", File.ReadAllText(path));

        File.WriteAllText(path, "old");
        Assert.True(Integration.DevKit.Core.FileUtils.WriteToFile(path, Array.Empty<string>(), append: true).MethodSuccess);
        Assert.Equal("old", File.ReadAllText(path));
    }

public class FileUtilsErrorTests
{
    [Fact]
    public void RethrownError_KeepsWhereItStarted()
    {
        // "throw result.Exception" reset the stack trace to the rethrowing line
        var write = Integration.DevKit.Core.FileUtils.WriteToFile("no-extension", new[] { "x" });

        Assert.False(write.MethodSuccess);
        Assert.Contains("RequireFilePath", write.Exception.StackTrace);
    }
}
}
