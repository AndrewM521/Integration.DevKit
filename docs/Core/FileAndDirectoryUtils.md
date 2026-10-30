# File and directory utilities

Part of [Integration.DevKit.Core](README.md).

`FileUtils` and `DirectoryUtils` wrap `System.IO` with the SDK's [result types](ResultTypes.md) and consistent path validation. Every operation validates its path first and fails with a descriptive exception rather than letting a raw `IOException` surface.

## Included types

Both are static classes in the `Integration.DevKit.Core` namespace:

| Type | What it does |
| --- | --- |
| `FileUtils` | Read, write, create, delete, copy and move files; validate file paths and extensions. |
| `DirectoryUtils` | Create and delete directories, list files, validate directory paths, and make safe directory names. |

## How it works

- **Path validation:** each method checks its path with `IsStringValidFilePath` or `IsStringValidDirectoryPath` before touching the disk.
- **File extensions:** by default, a path must end in an extension (e.g. `.txt`) to count as a valid *file* path. Pass `allowNoFileExtension: true` for extension-less files such as `Dockerfile` or `LICENSE`.
- **Writes:** create any missing parent directory automatically, and append a trailing newline if the content doesn't already end with one.

## How to

### Write and read a file

```csharp
using Integration.DevKit.Core;

var written = FileUtils.WriteToFile(path, "first line");
FileUtils.WriteToFile(path, new[] { "second", "third" }, append: true);

var text = FileUtils.ReadFileText(path);
if (text.MethodSuccess)
{
    Console.WriteLine(text.Result);
}
```

Every read and write has an async version (`WriteToFileAsync`, `ReadFileTextAsync`, etc.) with the same parameters.

### Copy, move and delete files

```csharp
FileUtils.CopyFile(source, destination, overwrite: true);
FileUtils.MoveFile(source, archivePath);
FileUtils.DeleteFiles(logFolder, "*.tmp");
```

### Work with directories

```csharp
DirectoryUtils.CreateDirectory(outputFolder);

var files = DirectoryUtils.GetFiles(outputFolder, SearchOption.AllDirectories, "*.json");

// Replace characters that aren't allowed in a directory name
var safe = DirectoryUtils.GetSafeDirectoryName("Report: 2026/10", replacement: '_');
```

### Share a file between threads or tasks

`FileUtils` does no locking. If the same file can be used from more than one place at once, use [`ThreadSafeFileIO`](../ThreadSafeItems/ThreadSafeFileIO.md) instead. It has the same method shapes.

## API reference

```csharp
public static class FileUtils
{
    // Write
    NullOperationResult WriteToFile(string path, string content, bool append = false, bool allowNoFileExtension = false, Encoding? encoding = null);
    NullOperationResult WriteToFile(string path, string[] content, bool append = false, bool allowNoFileExtension = false, Encoding? encoding = null);
    NullOperationResult WriteBytesToFile(string path, byte[] content, bool append = false, bool allowNoFileExtension = false);
    Task<NullOperationResult> WriteToFileAsync(string path, string content, bool append = false, bool allowNoFileExtension = false, Encoding? encoding = null);
    Task<NullOperationResult> WriteToFileAsync(string path, string[] content, bool append = false, bool allowNoFileExtension = false, Encoding? encoding = null);
    Task<NullOperationResult> WriteBytesToFileAsync(string path, byte[] content, bool append = false, bool allowNoFileExtension = false);

    // Read
    OperationResult<string> ReadFileText(string path, bool allowNoFileExtension = false);
    OperationResult<string[]> ReadFileLines(string path, bool allowNoFileExtension = false);
    OperationResult<byte[]> ReadFileBytes(string path, bool allowNoFileExtension = false);
    Task<OperationResult<string>> ReadFileTextAsync(string path, bool allowNoFileExtension = false);
    Task<OperationResult<string[]>> ReadFileLinesAsync(string path, bool allowNoFileExtension = false);
    Task<OperationResult<byte[]>> ReadFileBytesAsync(string path, bool allowNoFileExtension = false);

    // Manage
    NullOperationResult CreateFile(string path, bool allowNoFileExtension = false);
    NullOperationResult DeleteFile(string path, bool allowNoFileExtension = false);
    NullOperationResult DeleteFiles(string path, string searchPattern, bool allowNoFileExtension = false);
    NullOperationResult CopyFile(string sourcePath, string destinationPath, bool overwrite = false, bool allowNoFileExtension = false);
    NullOperationResult MoveFile(string sourcePath, string destinationPath, bool overwrite = false, bool allowNoFileExtension = false);

    // Inspect and validate
    OperationResult<string> GetExtension(string path, bool allowNoFileExtension = false);
    OperationResult<bool> IsStringValidFilePath(string path, bool allowNoFileExtension = false);
    OperationResult<bool> IsPathValidExtension(string filePath, string validExtension);
    bool DoesFileExist(string path);
}

public static class DirectoryUtils
{
    NullOperationResult CreateDirectory(string path);
    NullOperationResult DeleteDirectory(string path, bool recursive = false);
    OperationResult<string[]> GetFiles(string path, SearchOption searchOption = SearchOption.TopDirectoryOnly, string searchPattern = "*");
    OperationResult<bool> IsStringValidDirectoryPath(string path);
    OperationResult<string> GetSafeDirectoryName(string directoryName, char replacement = '_');
    bool DoesDirectoryExist(string path);
}
```

## Error handling

Methods return a failed result instead of throwing for expected failures, such as an invalid path, a missing file or access denied. Check `MethodSuccess` and read `Exception`. `DoesFileExist` and `DoesDirectoryExist` return a plain `bool`.

## Best practices

- Check `MethodSuccess` on every write. A failed write doesn't throw, so an unchecked result fails silently.
- Use `ThreadSafeFileIO` instead of `FileUtils` whenever the same file might be accessed concurrently.
- Pass `allowNoFileExtension: true` only for files that really have no extension, so path validation still catches typos elsewhere.
