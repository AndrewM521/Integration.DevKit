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
