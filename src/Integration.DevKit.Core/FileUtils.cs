using System.Text;
using System.Runtime.ExceptionServices;

namespace Integration.DevKit.Core;

/// <summary>
/// Utility methods for file operations, including reading, writing, moving, and validating file paths and extensions.
/// </summary>
public static class FileUtils
{
    private static readonly string RequiredDirectoryPathErrorMsg = "Path must be a Directory.";
    private static readonly string RequiredFilePathErrorMsg = "Path must be a File Path.";

    /// <summary>
    /// Throws unless <paramref name="path"/> is a usable file path. Shared by every method that takes a file path.
    /// </summary>
    private static void RequireFilePath(string path, bool allowNoFileExtension)
    {
        var validatePath = IsStringValidFilePath(path, allowNoFileExtension);
        if (!validatePath.MethodSuccess)
        {
            ExceptionDispatchInfo.Throw(validatePath.Exception);
        }

        if (!validatePath.Result)
        {
            throw new ArgumentException(RequiredFilePathErrorMsg);
        }
    }

    /// <summary>
    /// Shared start of every write: validates the path and creates its folder.
    /// Returns <see langword="false"/> when there is nothing to do (appending empty content).
    /// </summary>
    private static bool PrepareWrite(string path, bool allowNoFileExtension, bool isEmpty, bool append)
    {
        RequireFilePath(path, allowNoFileExtension);

        // Appending nothing is a no-op; overwriting with nothing must still truncate the file.
        if (isEmpty && append)
        {
            return false;
        }

        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            // Trailing separator so the validator treats new folders like "v1.2" as directories, not files
            var createDir = DirectoryUtils.CreateDirectory(directory + Path.DirectorySeparatorChar);
            if (!createDir.MethodSuccess)
            {
                ExceptionDispatchInfo.Throw(createDir.Exception);
            }
        }

        return true;
    }

    #region Asyncronous Methods
    /// <summary>
    /// Asynchronously writes a string to a file, ensuring the target directory exists.
    /// </summary>
    /// <param name="path">The destination file path.</param>
    /// <param name="content">The text content to write.</param>
    /// <param name="append"><see langword="true"/> to append to the file; <see langword="false"/> to overwrite. Defaults to <see langword="false"/>.</param>
    /// <param name="allowNoFileExtension">
    /// Optional flag to allow file names without an extension to pass validation 
    /// instead of requiring an explicit extension (e.g., '.txt').
    /// </param>
    /// <param name="encoding">The text encoding to use. Defaults to <see cref="Encoding.UTF8"/> if null.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating the status of the operation.</returns>
    public static async Task<NullOperationResult> WriteToFileAsync(string path, string content, bool append = false, bool allowNoFileExtension = false, Encoding? encoding = null)
    {
        var result = new NullOperationResult();
        Encoding encoder = Encoding.UTF8;

        try
        {
            if (encoding != null)
            {
                encoder = encoding;
            }

            if (content == null)
            {
                content = "";
            }

            if (!PrepareWrite(path, allowNoFileExtension, content.Length == 0, append))
            {
                return result.SetMethodSuccess();
            }

            if (content.Length > 0 && !content.EndsWith(Environment.NewLine))
            {
                content += Environment.NewLine;
            }

            var mode = FileMode.Create;

            if (append)
            {
                mode = FileMode.Append;
            }

            //FileShare: No other process or thread can open the file while this stream is open.
            //BufferSize: Buffer size for creating IO instructions. Windows default size is 4096, and windows combines multiple IO calls anyway, its not necessary to increase the size
            await using (var stream = new FileStream(path, mode, FileAccess.Write, FileShare.None, 4096, true))
            {
                await using (var writer = new StreamWriter(stream, encoder))
                {
                    // StreamWriter already encodes and flushes large strings in buffered blocks
                    await writer.WriteAsync(content).ConfigureAwait(false);
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
    /// <param name="allowNoFileExtension">
    /// Optional flag to allow file names without an extension to pass validation 
    /// instead of requiring an explicit extension (e.g., '.txt').
    /// </param>
    /// <param name="encoding">The text encoding to use. Defaults to <see cref="Encoding.UTF8"/> if null.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating the status of the operation.</returns>
    public static async Task<NullOperationResult> WriteToFileAsync(string path, string[] content, bool append = false, bool allowNoFileExtension = false, Encoding? encoding = null)
    {
        var result = new NullOperationResult();

        try
        {
            // Appending nothing is a no-op; overwriting with nothing must still truncate the file.
            if ((content == null || content.Length == 0) && append)
            {
                return result.SetMethodSuccess();
            }

            if (content == null)
            {
                content = Array.Empty<string>();
            }

            string combinedContent = string.Join(Environment.NewLine, content);

            var writeToFile = await WriteToFileAsync(path, combinedContent, append, allowNoFileExtension, encoding);
            if (!writeToFile.MethodSuccess)
            {
                ExceptionDispatchInfo.Throw(writeToFile.Exception);
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
    /// <param name="allowNoFileExtension">
    /// Optional flag to allow file names without an extension to pass validation 
    /// instead of requiring an explicit extension (e.g., '.txt').
    /// </param>
    /// <returns>A <see cref="NullOperationResult"/> indicating the status of the operation.</returns>
    public static async Task<NullOperationResult> WriteBytesToFileAsync(string path, byte[] content, bool append = false, bool allowNoFileExtension = false)
    {
        var result = new NullOperationResult();

        try
        {
            if (content == null)
            {
                content = Array.Empty<byte>();
            }

            if (!PrepareWrite(path, allowNoFileExtension, content.Length == 0, append))
            {
                return result.SetMethodSuccess();
            }

            var mode = FileMode.Create;

            if (append)
            {
                mode = FileMode.Append;
            }

            await using (var stream = new FileStream(path, mode, FileAccess.Write, FileShare.None, 4096, true))
            {
                await stream.WriteAsync(content).ConfigureAwait(false);
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
    /// <param name="allowNoFileExtension">
    /// Optional flag to allow file names without an extension to pass validation 
    /// instead of requiring an explicit extension (e.g., '.txt').
    /// </param>
    /// <returns>An <see cref="OperationResult{T}"/> containing an array of strings, one for each line.</returns>
    public static async Task<OperationResult<string[]>> ReadFileLinesAsync(string path, bool allowNoFileExtension = false)
    {
        var result = new OperationResult<string[]>();

        try
        {
            RequireFilePath(path, allowNoFileExtension);

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
    /// <param name="allowNoFileExtension">
    /// Optional flag to allow file names without an extension to pass validation 
    /// instead of requiring an explicit extension (e.g., '.txt').
    /// </param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the file's text.</returns>
    public static async Task<OperationResult<string>> ReadFileTextAsync(string path, bool allowNoFileExtension = false)
    {
        var result = new OperationResult<string>();

        try
        {
            RequireFilePath(path, allowNoFileExtension);

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
    /// <param name="allowNoFileExtension">
    /// Optional flag to allow file names without an extension to pass validation 
    /// instead of requiring an explicit extension (e.g., '.txt').
    /// </param>
    /// <returns>An <see cref="OperationResult{Byte[]}"/> containing the file's byte array.</returns>
    public static async Task<OperationResult<byte[]>> ReadFileBytesAsync(string path, bool allowNoFileExtension = false)
    {
        var result = new OperationResult<byte[]>();

        try
        {
            RequireFilePath(path, allowNoFileExtension);

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
    /// <param name="allowNoFileExtension">
    /// Optional flag to allow file names without an extension to pass validation 
    /// instead of requiring an explicit extension (e.g., '.txt').
    /// </param>
    /// <returns>A <see cref="NullOperationResult"/> indicating success or failure.</returns>
    public static NullOperationResult CreateFile(string path, bool allowNoFileExtension = false)
    {
        var result = new NullOperationResult();

        try
        {
            RequireFilePath(path, allowNoFileExtension);

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
    /// <param name="allowNoFileExtension">
    /// Optional flag to allow file names without an extension to pass validation 
    /// instead of requiring an explicit extension (e.g., '.txt').
    /// </param>
    /// <returns>A <see cref="NullOperationResult"/>.</returns>
    public static NullOperationResult DeleteFile(string path, bool allowNoFileExtension = false)
    {
        var result = new NullOperationResult();

        try
        {
            RequireFilePath(path, allowNoFileExtension);

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
    /// <param name="searchPattern">The search pattern (e.g., "*.log"). Matching files are deleted whether or not they have an extension.</param>
    /// <returns>A <see cref="NullOperationResult"/>.</returns>
    /// <exception cref="AggregateException">Thrown if one or more file deletions fail.</exception>
    public static NullOperationResult DeleteFiles(string path, string searchPattern)
    {
        var result = new NullOperationResult();

        try
        {
            // Nothing to delete; don't create the folder as a side effect
            if (!DirectoryUtils.DoesDirectoryExist(path))
            {
                return result.SetMethodSuccess();
            }

            var getFiles = DirectoryUtils.GetFiles(path, SearchOption.TopDirectoryOnly, searchPattern);
            if (!getFiles.MethodSuccess)
            {
                ExceptionDispatchInfo.Throw(getFiles.Exception);
            }

            var files = getFiles.Result;
            if (files.Length != 0)
            {
                List<Exception> errors = new List<Exception>();

                foreach (var file in files)
                {
                    // These came from the directory listing, so they are real files whether or not they have an extension
                    var fileResult = DeleteFile(file, allowNoFileExtension: true);
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
    /// <param name="allowNoFileExtension">
    /// Optional flag to allow file names without an extension to pass validation 
    /// instead of requiring an explicit extension (e.g., '.txt').
    /// </param>
    /// <returns>A <see cref="NullOperationResult"/>.</returns>
    public static NullOperationResult CopyFile(string sourcePath, string destinationPath, bool overwrite = false, bool allowNoFileExtension = false)
    {
        var result = new NullOperationResult();

        try
        {
            RequireFilePath(sourcePath, allowNoFileExtension);

            if (!DoesFileExist(sourcePath))
            {
                throw new FileNotFoundException("Source File Path does not exist.");
            }

            // An existing folder wins over the name check, so folders like "v1.2" aren't mistaken for files
            var validateDestinationPath = DirectoryUtils.IsStringValidDirectoryPath(destinationPath);
            if (!validateDestinationPath.MethodSuccess)
            {
                ExceptionDispatchInfo.Throw(validateDestinationPath.Exception);
            }

            if (!DirectoryUtils.DoesDirectoryExist(destinationPath) && !validateDestinationPath.Result)
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
    /// <param name="allowNoFileExtension">
    /// Optional flag to allow file names without an extension to pass validation 
    /// instead of requiring an explicit extension (e.g., '.txt').
    /// </param>
    /// <returns>A <see cref="NullOperationResult"/>.</returns>
    public static NullOperationResult MoveFile(string sourcePath, string destinationPath, bool overwrite = false, bool allowNoFileExtension = false)
    {
        var result = new NullOperationResult();

        try
        {
            RequireFilePath(sourcePath, allowNoFileExtension);

            // An existing folder wins over the name check, so folders like "v1.2" aren't mistaken for files
            bool isDir = true;
            var validateDestinationDirPath = DirectoryUtils.IsStringValidDirectoryPath(destinationPath);
            if (!validateDestinationDirPath.MethodSuccess)
            {
                ExceptionDispatchInfo.Throw(validateDestinationDirPath.Exception);
            }

            // With allowNoFileExtension, a missing destination without a trailing separator is the new file name (e.g. "LICENSE")
            bool isNewExtensionlessFile = allowNoFileExtension && !Path.EndsInDirectorySeparator(destinationPath);

            if (!DirectoryUtils.DoesDirectoryExist(destinationPath) && (!validateDestinationDirPath.Result || isNewExtensionlessFile))
            {
                isDir = false;

                var validateDestinationFilePath = IsStringValidFilePath(destinationPath, allowNoFileExtension);
                if (!validateDestinationFilePath.MethodSuccess)
                {
                    ExceptionDispatchInfo.Throw(validateDestinationFilePath.Exception);
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
    /// <param name="allowNoFileExtension">
    /// Optional flag to allow file names without an extension to pass validation 
    /// instead of requiring an explicit extension (e.g., '.txt').
    /// </param>
    /// <param name="encoding">The text encoding to use. Defaults to <see cref="Encoding.UTF8"/> if null.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating the status of the operation.</returns>
    public static NullOperationResult WriteToFile(string path, string content, bool append = false, bool allowNoFileExtension = false, Encoding? encoding = null)
    {
        var result = new NullOperationResult();
        Encoding encoder = Encoding.UTF8;

        try
        {
            if (encoding != null)
            {
                encoder = encoding;
            }

            if (content == null)
            {
                content = "";
            }

            if (!PrepareWrite(path, allowNoFileExtension, content.Length == 0, append))
            {
                return result.SetMethodSuccess();
            }

            if (content.Length > 0 && !content.EndsWith(Environment.NewLine))
            {
                content += Environment.NewLine;
            }

            var mode = FileMode.Create;

            if (append)
            {
                mode = FileMode.Append;
            }

            //FileShare: No other process or thread can open the file while this stream is open.
            //BufferSize: Buffer size for creating IO instructions. Windows default size is 4096, and windows combines multiple IO calls anyway, its not necessary to increase the size

            // Note: useAsync: false is the key here
            using (var stream = new FileStream(path, mode, FileAccess.Write, FileShare.None, 4096, useAsync: false))
            {
                using (var writer = new StreamWriter(stream, encoder))
                {
                    // StreamWriter already encodes and flushes large strings in buffered blocks
                    writer.Write(content);
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
    /// <param name="allowNoFileExtension">
    /// Optional flag to allow file names without an extension to pass validation 
    /// instead of requiring an explicit extension (e.g., '.txt').
    /// </param>
    /// <param name="encoding">The text encoding to use. Defaults to <see cref="Encoding.UTF8"/> if null.</param>
    /// <returns>A <see cref="NullOperationResult"/>.</returns>
    public static NullOperationResult WriteToFile(string path, string[] content, bool append = false, bool allowNoFileExtension = false, Encoding? encoding = null)
    {
        var result = new NullOperationResult();

        try
        {
            // Appending nothing is a no-op; overwriting with nothing must still truncate the file.
            if ((content == null || content.Length == 0) && append)
            {
                return result.SetMethodSuccess();
            }

            if (content == null)
            {
                content = Array.Empty<string>();
            }

            string combinedContent = string.Join(Environment.NewLine, content);

            var writeToFile = WriteToFile(path, combinedContent, append, allowNoFileExtension, encoding);
            if (!writeToFile.MethodSuccess)
            {
                ExceptionDispatchInfo.Throw(writeToFile.Exception);
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
    /// <param name="allowNoFileExtension">
    /// Optional flag to allow file names without an extension to pass validation 
    /// instead of requiring an explicit extension (e.g., '.txt').
    /// </param>
    /// <returns>A <see cref="NullOperationResult"/> indicating the status of the operation.</returns>
    public static NullOperationResult WriteBytesToFile(string path, byte[] content, bool append = false, bool allowNoFileExtension = false)
    {
        var result = new NullOperationResult();

        try
        {
            if (content == null)
            {
                content = Array.Empty<byte>();
            }

            if (!PrepareWrite(path, allowNoFileExtension, content.Length == 0, append))
            {
                return result.SetMethodSuccess();
            }

            var mode = FileMode.Create;

            if (append)
            {
                mode = FileMode.Append;
            }

            using (var stream = new FileStream(path, mode, FileAccess.Write, FileShare.None, 4096, useAsync: false))
            {
                stream.Write(content);
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
    /// <param name="allowNoFileExtension">
    /// Optional flag to allow file names without an extension to pass validation 
    /// instead of requiring an explicit extension (e.g., '.txt').
    /// </param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the array of lines.</returns>
    public static OperationResult<string[]> ReadFileLines(string path, bool allowNoFileExtension = false)
    {
        var result = new OperationResult<string[]>();
        try
        {
            RequireFilePath(path, allowNoFileExtension);

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
    /// <param name="allowNoFileExtension">
    /// Optional flag to allow file names without an extension to pass validation 
    /// instead of requiring an explicit extension (e.g., '.txt').
    /// </param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the file text.</returns>
    public static OperationResult<string> ReadFileText(string path, bool allowNoFileExtension = false)
    {
        var result = new OperationResult<string>();
        try
        {
            RequireFilePath(path, allowNoFileExtension);

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
    /// <param name="allowNoFileExtension">
    /// Optional flag to allow file names without an extension to pass validation 
    /// instead of requiring an explicit extension (e.g., '.txt').
    /// </param>
    /// <returns>An <see cref="OperationResult{Byte[]}"/> containing the file's byte array.</returns>
    public static OperationResult<byte[]> ReadFileBytes(string path, bool allowNoFileExtension = false)
    {
        var result = new OperationResult<byte[]>();

        try
        {
            RequireFilePath(path, allowNoFileExtension);

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
    /// <param name="allowNoFileExtension">
    /// Optional flag to allow file names without an extension to pass validation 
    /// instead of requiring an explicit extension (e.g., '.txt').
    /// </param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the extension (e.g., ".json").</returns>
    public static OperationResult<string> GetExtension(string path, bool allowNoFileExtension = false)
    {
        var result = new OperationResult<string>();

        try
        {
            RequireFilePath(path, allowNoFileExtension);

            string extension = Path.GetExtension(path).Trim().ToLowerInvariant();

            return result.SetMethodSuccess(extension);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Performs a heuristic check to determine if a string is formatted as a valid file path.
    /// </summary>
    /// <param name="path">The string path to validate.</param>
    /// <param name="allowNoFileExtension">If <see langword="true"/>, paths targeting files without an extension (e.g., 'Dockerfile', 'LICENSE') are considered valid.</param>
    /// <returns>
    /// An <see cref="OperationResult{T}"/> where the result is <see langword="true"/> if the string is structurally valid to be a file path.
    /// </returns>
    public static OperationResult<bool> IsStringValidFilePath(string path, bool allowNoFileExtension = false)
    {
        var result = new OperationResult<bool>();

        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return result.SetMethodSuccess(false);
            }

            if (path.IndexOfAny(Path.GetInvalidPathChars()) != -1)
            {
                return result.SetMethodSuccess(false);
            }

            var trimmedPath = path.TrimEnd();
            if (trimmedPath.EndsWith(Path.DirectorySeparatorChar) || trimmedPath.EndsWith(Path.AltDirectorySeparatorChar))
            {
                return result.SetMethodSuccess(false);
            }

            string fullPath = Path.GetFullPath(path);
            string fileName = Path.GetFileName(fullPath);

            if (string.IsNullOrEmpty(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                return result.SetMethodSuccess(false);
            }

            // Ensure it actually has a file extension (e.g., ".txt")
            if (!allowNoFileExtension && !Path.HasExtension(fileName))
            {
                return result.SetMethodSuccess(false);
            }

            return result.SetMethodSuccess(true);
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
                ExceptionDispatchInfo.Throw(validateFile.Exception);
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
