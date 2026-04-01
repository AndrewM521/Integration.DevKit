using AndrewM5.DevKit.Core.Results;
using System.Text;

namespace AndrewM5.DevKit.Core;

/// <summary>
/// Provides a comprehensive set of utility methods for file operations, including reading, 
/// writing, moving, and validating file paths and extensions.
/// </summary>
public static class FileExtension
{
    private static readonly string RequiredDirectoryPathErrorMsg = "Path must be a Directory.";
    private static readonly string RequiredFilePathErrorMsg = "Path must be a File Path.";

    #region Asyncronous Methods
    /// <summary>
    /// Asynchronously writes a string to a file.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <param name="content">The text content to write.</param>
    /// <param name="append"><c>true</c> to append to an existing file; <c>false</c> to overwrite. Defaults to <c>true</c></param>
    /// <param name="encoding">The text encoding to use. Defaults to UTF-8 if null.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating the result of the operation.</returns>
    /// <remarks>
    /// This method automatically ensures the parent directory exists and writes the file in 100MB chunks 
    /// to handle large strings efficiently without high memory pressure.
    /// </remarks>
    public static async Task<NullOperationResult> WriteToFileAsync(string path, string content, bool append = false, Encoding? encoding = null)
    {
        var result = new NullOperationResult();
        Encoding encoder = Encoding.UTF8;

        try
        {
            if (encoding != null)
            {
                encoder = encoding;
            }

            var validatePath = IsStringValidFilePath(path);
            if (!validatePath.MethodSuccess)
            {
                throw validatePath.Exception;
            }

            if (!validatePath.Result)
            {
                throw new ArgumentException(RequiredFilePathErrorMsg);
            }

            if (string.IsNullOrEmpty(content))
            {
                return result.SetMethodSuccess();
            }

            var directory = Path.GetDirectoryName(path);
            var createDir = DirectoryUtils.CreateDirectory(directory!);
            if (!createDir.MethodSuccess)
            {
                throw createDir.Exception;
            }

            if (!content.EndsWith(Environment.NewLine))
            {
                content += Environment.NewLine;
            }

            var mode = FileMode.Create;

            if (append)
            {
                mode = FileMode.Append;
            }

            long maxChunkSizeInBytes = 100 * 1024 * 1024;
            int startIndex = 0;

            //FileShare: No other process or thread can open the file while this stream is open.
            //BufferSize: Buffer size for creating IO instructions. Windows default size is 4096, and windows combines multiple IO calls anyway, its not necessary to increase the size
            await using (var stream = new FileStream(path, mode, FileAccess.Write, FileShare.None, 4096, true))
            {
                await using (var writer = new StreamWriter(stream, encoder))
                {
                    while (startIndex < content.Length)
                    {
                        int length = 0;
                        long byteCount = 0;

                        while (startIndex + length < content.Length)
                        {
                            char character = content[startIndex + length];
                            long bytes = encoder.GetByteCount(new[] { character });

                            if (byteCount + bytes > maxChunkSizeInBytes)
                            {
                                break;
                            }

                            byteCount += bytes;
                            length++;
                        }

                        string chunk = content.Substring(startIndex, length);

                        await writer.WriteAsync(chunk).ConfigureAwait(false);

                        startIndex += length;
                    }
                }
            }

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Asynchronously writes an array of strings to a file, joining them with new lines.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <param name="content">The array of strings to write.</param>
    /// <param name="append"><c>true</c> to append to an existing file; <c>false</c> to overwrite. Defaults to <c>false</c></param>
    /// <param name="encoding">The text encoding to use. Defaults to UTF-8 if null.</param>
    /// <returns>A <see cref="NullOperationResult"/>.</returns>
    /// <remarks>
    /// This method automatically ensures the parent directory exists and writes the file in 100MB chunks 
    /// to handle large strings efficiently without high memory pressure.
    /// </remarks>
    public static async Task<NullOperationResult> WriteToFileAsync(string path, string[] content, bool append = false, Encoding? encoding = null)
    {
        var result = new NullOperationResult();

        try
        {
            if (content == null || content.Length == 0)
            {
                return result.SetMethodSuccess();
            }

            string combinedContent = string.Join(Environment.NewLine, content);

            var writeToFile = await WriteToFileAsync(path, combinedContent, append, encoding);
            if (!writeToFile.MethodSuccess)
            {
                throw writeToFile.Exception;
            }

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Asynchronously reads all lines from a file.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the array of lines.</returns>
    public static async Task<OperationResult<string[]>> ReadFileLinesAsync(string path)
    {
        var result = new OperationResult<string[]>();

        try
        {
            var validatePath = IsStringValidFilePath(path);
            if (!validatePath.MethodSuccess)
            {
                throw validatePath.Exception;
            }

            if (!validatePath.Result)
            {
                throw new ArgumentException(RequiredFilePathErrorMsg);
            }

            string[] lines = await File.ReadAllLinesAsync(path);

            return result.SetMethodSuccess(lines);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Asynchronously reads the content of the file.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the file text.</returns>
    public static async Task<OperationResult<string>> ReadFileTextAsync(string path)
    {
        var result = new OperationResult<string>();

        try
        {
            var validatePath = IsStringValidFilePath(path);
            if (!validatePath.MethodSuccess)
            {
                throw validatePath.Exception;
            }

            if (!validatePath.Result)
            {
                throw new ArgumentException(RequiredFilePathErrorMsg);
            }

            string line = await File.ReadAllTextAsync(path);

            return result.SetMethodSuccess(line);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }
    #endregion

    #region Syncronous Methods
    /// <summary>
    /// Creates a new file at the specified path.
    /// </summary>
    /// <param name="path">The path to create the file.</param>
    /// <returns>A <see cref="NullOperationResult"/>.</returns>
    public static NullOperationResult CreateFile(string path)
    {
        var result = new NullOperationResult();

        try
        {
            var validatePath = IsStringValidFilePath(path);
            if (!validatePath.MethodSuccess)
            {
                throw validatePath.Exception;
            }

            if (!validatePath.Result)
            {
                throw new ArgumentException(RequiredFilePathErrorMsg);
            }

            File.Create(path).Dispose();

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Deletes a file if it exists.
    /// </summary>
    /// <param name="path">The path to the file to delete.</param>
    /// <returns>A <see cref="NullOperationResult"/>.</returns>
    public static NullOperationResult DeleteFile(string path)
    {
        var result = new NullOperationResult();

        try
        {
            var validatePath = IsStringValidFilePath(path);
            if (!validatePath.MethodSuccess)
            {
                throw validatePath.Exception;
            }

            if (!validatePath.Result)
            {
                throw new ArgumentException(RequiredFilePathErrorMsg);
            }

            if (DoesFileExist(path))
            {
                File.Delete(path);
            }

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Deletes multiple files in a directory that match a search pattern.
    /// </summary>
    /// <param name="path">The directory path.</param>
    /// <param name="searchPattern">The pattern to match files (e.g. "*.tmp").</param>
    /// <returns>A <see cref="NullOperationResult"/>.</returns>
    /// <exception cref="AggregateException">Thrown if one or more files fail to be deleted.</exception>
    public static NullOperationResult DeleteFiles(string path, string searchPattern)
    {
        var result = new NullOperationResult();

        try
        {
            if (!DirectoryUtils.DoesDirectoryExist(path))
            {
                DirectoryUtils.CreateDirectory(path);
            }

            var getFiles = DirectoryUtils.GetFiles(path, searchPattern);
            if (!getFiles.MethodSuccess)
            {
                throw getFiles.Exception;
            }

            var files = getFiles.Result;
            if (files.Length != 0)
            {
                List<Exception> errors = new List<Exception>();

                foreach (var file in files)
                {
                    var fileResult = DeleteFile(file);
                    if (!fileResult.MethodSuccess)
                    {
                        errors.Add(fileResult.Exception);
                    }
                }

                if (errors.Count != 0)
                {
                    throw new AggregateException(errors);
                }
            }

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Copies an existing file to a new directory.
    /// </summary>
    /// <param name="sourcePath">The path of the file to copy.</param>
    /// <param name="destinationPath">The directory path to copy the file to.</param>
    /// <param name="overwrite"><c>true</c> if the destination file can be overwritten; otherwise, <c>false</c>. Defaults to <c>false</c></param>
    /// <returns>A <see cref="NullOperationResult"/>.</returns>
    public static NullOperationResult CopyFile(string sourcePath, string destinationPath, bool overwrite = false)
    {
        var result = new NullOperationResult();

        try
        {
            var validateSourcePath = IsStringValidFilePath(sourcePath);
            if (!validateSourcePath.MethodSuccess)
            {
                throw validateSourcePath.Exception;
            }

            if (!validateSourcePath.Result)
            {
                throw new ArgumentException(RequiredFilePathErrorMsg);
            }

            if (!DoesFileExist(sourcePath))
            {
                throw new FileNotFoundException("Source File Path does not exist.");
            }

            var validateDestinationPath = DirectoryUtils.IsStringValidDirectoryPath(destinationPath);
            if (!validateDestinationPath.MethodSuccess)
            {
                throw validateDestinationPath.Exception;
            }

            if (!validateDestinationPath.Result)
            {
                throw new ArgumentException(RequiredDirectoryPathErrorMsg);
            }

            string destinationFilePath = Path.Combine(destinationPath, Path.GetFileName(sourcePath));

            File.Copy(sourcePath, destinationFilePath, overwrite);

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Moves a file to a new location, supporting both directory and file-specific destination paths.
    /// </summary>
    /// <param name="sourcePath">The current path of the file.</param>
    /// <param name="destinationPath">The destination directory or new file path.</param>
    /// <param name="overwrite"><c>true</c> to overwrite the destination if it exists. Defaults to <c>false</c></param>
    /// <returns>A <see cref="NullOperationResult"/>.</returns>
    public static NullOperationResult MoveFile(string sourcePath, string destinationPath, bool overwrite = false)
    {
        var result = new NullOperationResult();

        try
        {
            var validateSourcePath = IsStringValidFilePath(sourcePath);
            if (!validateSourcePath.MethodSuccess)
            {
                throw validateSourcePath.Exception;
            }

            if (!validateSourcePath.Result)
            {
                throw new ArgumentException(RequiredFilePathErrorMsg);
            }

            bool isDir = true;
            var validateDestinationDirPath = DirectoryUtils.IsStringValidDirectoryPath(destinationPath);
            if (!validateDestinationDirPath.MethodSuccess)
            {
                throw validateDestinationDirPath.Exception;
            }

            if (!validateDestinationDirPath.Result)
            {
                isDir = false;

                var validateDestinationFilePath = IsStringValidFilePath(destinationPath);
                if (!validateDestinationFilePath.MethodSuccess)
                {
                    throw validateDestinationFilePath.Exception;
                }

                if (!validateDestinationFilePath.Result)
                {
                    throw new ArgumentException("DestinationPath is neither a valid file path or directory path.");
                }
            }

            string destinationFilePath = destinationPath;
            
            if (isDir)
            {
                destinationFilePath = Path.Combine(destinationPath, Path.GetFileName(sourcePath));
            }

            File.Move(sourcePath, destinationFilePath, overwrite);

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Synchronously writes a string to a file.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <param name="content">The text content to write.</param>
    /// <param name="append"><c>true</c> to append to an existing file; <c>false</c> to overwrite. Defaults to <c>true</c></param>
    /// <param name="encoding">The text encoding to use. Defaults to UTF-8 if null.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating the result of the operation.</returns>
    /// <remarks>
    /// This method automatically ensures the parent directory exists and writes the file in 100MB chunks 
    /// to handle large strings efficiently without high memory pressure.
    /// </remarks>
    public static NullOperationResult WriteToFile(string path, string content, bool append = false, Encoding? encoding = null)
    {
        return WriteToFileAsync(path, content, append, encoding).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Synchronously writes an array of strings to a file.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <param name="content">The array of strings to write.</param>
    /// <param name="append"><c>true</c> to append to an existing file; <c>false</c> to overwrite. Defaults to <c>false</c></param>
    /// <param name="encoding">The text encoding to use. Defaults to UTF-8 if null.</param>
    /// <returns>A <see cref="NullOperationResult"/>.</returns>
    /// <remarks>
    /// This method automatically ensures the parent directory exists and writes the file in 100MB chunks 
    /// to handle large strings efficiently without high memory pressure.
    /// </remarks>
    public static NullOperationResult WriteToFile(string path, string[] content, bool append = false, Encoding? encoding = null)
    {
        return WriteToFileAsync(path, content, append, encoding).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Synchronously reads all lines from a file.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the array of lines.</returns>
    public static OperationResult<string[]> ReadFileLines(string path)
    {
        return ReadFileLinesAsync(path).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Synchronously reads the entire text of a file.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the file text.</returns>
    public static OperationResult<string> ReadFileText(string path)
    {
        return ReadFileTextAsync(path).GetAwaiter().GetResult();
    }

    /// <summary>
    /// Gets the file extension from a path, normalized to lowercase.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the extension (e.g., ".txt").</returns>
    public static OperationResult<string> GetExtension(string path)
    {
        var result = new OperationResult<string>();

        try
        {
            var validatePath = IsStringValidFilePath(path);
            if (!validatePath.MethodSuccess)
            {
                throw validatePath.Exception;
            }

            if (!validatePath.Result)
            {
                throw new ArgumentException(RequiredFilePathErrorMsg);
            }

            string extension = Path.GetExtension(path).Trim().ToLowerInvariant();

            return result.SetMethodSuccess(extension);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Validates if a string follows a valid file path format.
    /// </summary>
    /// <param name="path">The path string to validate.</param>
    /// <returns>
    /// An <see cref="OperationResult{T}"/> where the result is <c>true</c> if the path has an 
    /// extension and a valid file name.
    /// </returns>
    public static OperationResult<bool> IsStringValidFilePath(string path)
    {
        var result = new OperationResult<bool>();

        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be null or whitespace.");
            }

            string fileName = Path.GetFileName(path);
            if (fileName.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                throw new ArgumentException("File name contains invalid characters.");
            }

            bool isFile = false;

            if (Path.HasExtension(path) && !string.IsNullOrWhiteSpace(fileName))
            {
                isFile = true;
            }

            return result.SetMethodSuccess(isFile);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Checks if a file path matches a specific extension.
    /// </summary>
    /// <param name="filePath">The path to check.</param>
    /// <param name="validExtension">The extension to compare against (e.g., ".json").</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing <c>true</c> if they match.</returns>
    public static OperationResult<bool> IsPathValidExtension(string filePath, string validExtension)
    {
        var result = new OperationResult<bool>();

        try
        {
            var validateFile = GetExtension(filePath);
            if (!validateFile.MethodSuccess)
            {
                throw validateFile.Exception;
            }

            bool isCorrectExtension = false;

            if (validateFile.Result.Trim().Equals(validExtension, StringComparison.OrdinalIgnoreCase))
            {
                isCorrectExtension = true;
            }

            return result.SetMethodSuccess(isCorrectExtension);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Determines whether the specified file exists.
    /// </summary>
    /// <param name="path">The path to test.</param>
    /// <returns><c>true</c> if the file exists; otherwise, <c>false</c>.</returns>
    public static bool DoesFileExist(string path)
    {
        return File.Exists(path);
    }
    #endregion
}
