using System.Text;

namespace Integration.DevKit.Core;

/// <summary>
/// Utility methods for file operations, including reading, writing, moving, and validating file paths and extensions.
/// </summary>
public static class FileUtils
{
    private static readonly string RequiredDirectoryPathErrorMsg = "Path must be a Directory.";
    private static readonly string RequiredFilePathErrorMsg = "Path must be a File Path.";

    #region Asyncronous Methods
    /// <summary>
    /// Asynchronously writes a string to a file, ensuring the target directory exists.
    /// </summary>
    /// <param name="path">The destination file path.</param>
    /// <param name="content">The text content to write.</param>
    /// <param name="append"><see langword="true"/> to append to the file; <see langword="false"/> to overwrite. Defaults to <see langword="false"/>.</param>
    /// <param name="encoding">The text encoding to use. Defaults to <see cref="Encoding.UTF8"/> if null.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating the status of the operation.</returns>
    /// <remarks>
    /// This method performs several automatic actions:
    /// <list type="bullet">
    /// <item>Creates the parent directory if it does not exist.</item>
    /// <item>Ensures the content ends with <see cref="Environment.NewLine"/>.</item>
    /// <item>Writes data in 100MB chunks to optimize memory usage for large strings.</item>
    /// <item>Locks the file (<see cref="FileShare.None"/>) during the write operation.</item>
    /// </list>
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
    /// Asynchronously writes an array of strings to a file, joining them with line breaks.
    /// </summary>
    /// <param name="path">The destination file path.</param>
    /// <param name="content">The array of strings to write.</param>
    /// <param name="append"><see langword="true"/> to append; <see langword="false"/> to overwrite.</param>
    /// <param name="encoding">The text encoding to use. Defaults to <see cref="Encoding.UTF8"/> if null.</param>
    /// <returns>A <see cref="NullOperationResult"/>.</returns>
    /// <remarks>
    /// This method internally calls <see cref="WriteToFileAsync(string, string, bool, Encoding?)"/> after 
    /// joining the array elements with <see cref="Environment.NewLine"/>. 
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
    /// Asynchronously writes a byte array to a specified file path
    /// </summary>
    /// <param name="path">The file path where the bytes will be written.</param>
    /// <param name="content">The byte array containing the data to write to the file.</param>
    /// <param name="append"><see langword="true"/> to append the data to the end of the file; <see langword="false"/> to overwrite or create a new file.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating the status of the operation.</returns>
    public static async Task<NullOperationResult> WriteBytesToFileAsync(string path, byte[] content, bool append = false)
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

            if (content == null || content.Length == 0)
            {
                return result.SetMethodSuccess();
            }

            var directory = Path.GetDirectoryName(path);
            var createDir = DirectoryUtils.CreateDirectory(directory!);
            if (!createDir.MethodSuccess)
            {
                throw createDir.Exception;
            }

            var mode = append ? FileMode.Append : FileMode.Create;

            // 100 MB chunk size in bytes
            int maxChunkSizeInBytes = 100 * 1024 * 1024;
            int startIndex = 0;

            // 5. Asynchronous File Stream Writing
            await using (var stream = new FileStream(path, mode, FileAccess.Write, FileShare.None, 4096, true))
            {
                while (startIndex < content.Length)
                {
                    // Calculate how many bytes to write in this chunk
                    int bytesLeft = content.Length - startIndex;
                    int chunkSize = Math.Min(bytesLeft, maxChunkSizeInBytes);

                    // Write the chunk memory directly to the stream
                    var chunk = new ReadOnlyMemory<byte>(content, startIndex, chunkSize);
                    await stream.WriteAsync(chunk).ConfigureAwait(false);

                    startIndex += chunkSize;
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
    /// Asynchronously reads all lines from the specified file.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing an array of strings, one for each line.</returns>
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
    /// Asynchronously reads the entire text content of a file.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the file's text.</returns>
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

    /// <summary>
    /// Asynchronously reads the entire byte content of a file.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>An <see cref="OperationResult{Byte[]}"/> containing the file's byte array.</returns>
    public static async Task<OperationResult<byte[]>> ReadFileBytesAsync(string path)
    {
        var result = new OperationResult<byte[]>();

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

            // Read the file as raw binary data
            byte[] bytes = await File.ReadAllBytesAsync(path);

            return result.SetMethodSuccess(bytes);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }
    #endregion

    #region Syncronous Methods
    /// <summary>
    /// Creates a new empty file at the specified path.
    /// </summary>
    /// <param name="path">The path to create the file.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating success or failure.</returns>
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
    /// Deletes a file from the file system if it exists.
    /// </summary>
    /// <param name="path">The path to the file.</param>
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
    /// Deletes all files in a directory that match a specific search pattern.
    /// </summary>
    /// <param name="path">The directory containing the files.</param>
    /// <param name="searchPattern">The search pattern (e.g., "*.log").</param>
    /// <returns>A <see cref="NullOperationResult"/>.</returns>
    /// <exception cref="AggregateException">Thrown if one or more file deletions fail.</exception>
    public static NullOperationResult DeleteFiles(string path, string searchPattern)
    {
        var result = new NullOperationResult();

        try
        {
            if (!DirectoryUtils.DoesDirectoryExist(path))
            {
                DirectoryUtils.CreateDirectory(path);
            }

            var getFiles = DirectoryUtils.GetFiles(path, SearchOption.TopDirectoryOnly, searchPattern);
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
    /// Copies a file to a target directory.
    /// </summary>
    /// <param name="sourcePath">The path of the file to copy.</param>
    /// <param name="destinationPath">The destination directory path.</param>
    /// <param name="overwrite"><see langword="true"/> to overwrite an existing file; otherwise, <see langword="false"/>.</param>
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
    /// Moves a file to a new location.
    /// </summary>
    /// <param name="sourcePath">The current file path.</param>
    /// <param name="destinationPath">The destination directory or new file path.</param>
    /// <param name="overwrite"><see langword="true"/> to overwrite; otherwise, <see langword="false"/>.</param>
    /// <returns>A <see cref="NullOperationResult"/>.</returns>
    /// <remarks>
    /// This method detects if <paramref name="destinationPath"/> is a directory or a specific file path 
    /// and handles the move accordingly.
    /// </remarks>
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
    /// Synchronously writes a string to a file, ensuring the target directory exists.
    /// </summary>
    /// <param name="path">The destination file path.</param>
    /// <param name="content">The text content to write.</param>
    /// <param name="append"><see langword="true"/> to append to the file; <see langword="false"/> to overwrite. Defaults to <see langword="false"/>.</param>
    /// <param name="encoding">The text encoding to use. Defaults to <see cref="Encoding.UTF8"/> if null.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating the status of the operation.</returns>
    /// <remarks>
    /// This method performs several automatic actions:
    /// <list type="bullet">
    /// <item>Creates the parent directory if it does not exist.</item>
    /// <item>Ensures the content ends with <see cref="Environment.NewLine"/>.</item>
    /// <item>Writes data in 100MB chunks to optimize memory usage for large strings.</item>
    /// <item>Locks the file (<see cref="FileShare.None"/>) during the write operation.</item>
    /// </list>
    /// </remarks>
    public static NullOperationResult WriteToFile(string path, string content, bool append = false, Encoding? encoding = null)
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

            // Note: useAsync: false is the key here
            using (var stream = new FileStream(path, mode, FileAccess.Write, FileShare.None, 4096, useAsync: false))
            {
                using (var writer = new StreamWriter(stream, encoding ?? Encoding.UTF8))
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

                        writer.Write(chunk);
                        writer.Flush();

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
    /// Synchronously writes an array of strings to a file, joining them with line breaks.
    /// </summary>
    /// <param name="path">The destination file path.</param>
    /// <param name="content">The array of strings to write.</param>
    /// <param name="append"><see langword="true"/> to append; <see langword="false"/> to overwrite.</param>
    /// <param name="encoding">The text encoding to use. Defaults to <see cref="Encoding.UTF8"/> if null.</param>
    /// <returns>A <see cref="NullOperationResult"/>.</returns>
    /// <remarks>
    /// This method internally calls <see cref="WriteToFile(string, string, bool, Encoding?)"/> after 
    /// joining the array elements with <see cref="Environment.NewLine"/>. 
    /// </remarks>
    public static NullOperationResult WriteToFile(string path, string[] content, bool append = false, Encoding? encoding = null)
    {
        var result = new NullOperationResult();

        try
        {
            if (content == null || content.Length == 0)
            {
                return result.SetMethodSuccess();
            }

            string combinedContent = string.Join(Environment.NewLine, content);

            var writeToFile = WriteToFile(path, combinedContent, append, encoding);
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
    /// Synchronously writes a byte array to a specified file path
    /// </summary>
    /// <param name="path">The file path where the bytes will be written.</param>
    /// <param name="content">The byte array containing the data to write to the file.</param>
    /// <param name="append"><see langword="true"/> to append the data to the end of the file; <see langword="false"/> to overwrite or create a new file.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating the status of the operation.</returns>
    public static NullOperationResult WriteBytesToFile(string path, byte[] content, bool append = false)
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

            if (content == null || content.Length == 0)
            {
                return result.SetMethodSuccess();
            }

            var directory = Path.GetDirectoryName(path);
            var createDir = DirectoryUtils.CreateDirectory(directory!);
            if (!createDir.MethodSuccess)
            {
                throw createDir.Exception;
            }

            var mode = append ? FileMode.Append : FileMode.Create;

            // 100 MB chunk size in bytes
            int maxChunkSizeInBytes = 100 * 1024 * 1024;
            int startIndex = 0;

            using (var stream = new FileStream(path, mode, FileAccess.Write, FileShare.None, 4096, useAsync: false))
            {
                while (startIndex < content.Length)
                {
                    // Calculate how many bytes to write in this chunk
                    int bytesLeft = content.Length - startIndex;
                    int chunkSize = Math.Min(bytesLeft, maxChunkSizeInBytes);

                    // Write the chunk directly from the array using an offset and count
                    stream.Write(content, startIndex, chunkSize);

                    startIndex += chunkSize;
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
    /// Synchronously reads all lines from a file.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the array of lines.</returns>
    public static OperationResult<string[]> ReadFileLines(string path)
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

            string[] lines = File.ReadAllLines(path);

            return result.SetMethodSuccess(lines);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Synchronously reads the entire text of a file.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the file text.</returns>
    public static OperationResult<string> ReadFileText(string path)
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

            string lines = File.ReadAllText(path);

            return result.SetMethodSuccess(lines);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }


    /// <summary>
    /// Synchronously reads the entire byte content of a file.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns>An <see cref="OperationResult{Byte[]}"/> containing the file's byte array.</returns>
    public static OperationResult<byte[]> ReadFileBytes(string path)
    {
        var result = new OperationResult<byte[]>();

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

            // Read the file as raw binary data
            byte[] bytes = File.ReadAllBytes(path);

            return result.SetMethodSuccess(bytes);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Extracts the file extension from a path, normalized to lowercase.
    /// </summary>
    /// <param name="path">The file path.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the extension (e.g., ".json").</returns>
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
    /// Validates if a string matches a valid file path format (has an extension and a filename).
    /// </summary>
    /// <param name="path">The path string to validate.</param>
    /// <returns>
    /// An <see cref="OperationResult{T}"/> containing <see langword="true"/> if the format is valid.
    /// </returns>
    /// <remarks>
    /// This method does not check if the file actually exists on disk; it only validates the 
    /// string structure and checks for invalid characters.
    /// </remarks>
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
    /// Validates if a file path matches a specific extension (case-insensitive).
    /// </summary>
    /// <param name="filePath">The file path to check.</param>
    /// <param name="validExtension">The target extension (e.g., ".txt").</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing <see langword="true"/> if the extensions match.</returns>
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
    /// Checks if a file exists on the disk.
    /// </summary>
    /// <param name="path">The path to the file.</param>
    /// <returns><see langword="true"/> if the file exists; otherwise, <see langword="false"/>.</returns>
    public static bool DoesFileExist(string path)
    {
        return File.Exists(path);
    }
    #endregion
}
