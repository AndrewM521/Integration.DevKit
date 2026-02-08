using AndrewM5.DevKit.Core.Results;
using System.Text;

namespace AndrewM5.DevKit.Core;

public static class FileExtension
{
    private static readonly string RequiredDirectoryPathErrorMsg = "Path must be a Directory.";
    private static readonly string RequiredFilePathErrorMsg = "Path must be a File Path.";

    #region Asyncronous Methods
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
            var createDir = DirectoryExtension.CreateDirectory(directory!);
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
    
    public static async Task<OperationResult<string[]>> ReadFileAsync(string path)
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
    #endregion

    #region Syncronous Methods
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
    
    public static NullOperationResult DeleteFiles(string path, string searchPattern)
    {
        var result = new NullOperationResult();

        try
        {
            if (!DirectoryExtension.DoesDirectoryExist(path))
            {
                DirectoryExtension.CreateDirectory(path);
            }

            var getFiles = DirectoryExtension.GetFiles(path, searchPattern);
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

            var validateDestinationPath = DirectoryExtension.IsStringValidDirectoryPath(destinationPath);
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

            var validateDestinationPath = DirectoryExtension.IsStringValidDirectoryPath(destinationPath);
            if (!validateDestinationPath.MethodSuccess)
            {
                throw validateDestinationPath.Exception;
            }

            if (!validateDestinationPath.Result)
            {
                throw new ArgumentException(RequiredDirectoryPathErrorMsg);
            }

            string destinationFilePath = Path.Combine(destinationPath, Path.GetFileName(sourcePath));

            File.Move(sourcePath, destinationFilePath, overwrite);

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }
    
    public static NullOperationResult WriteToFile(string path, string content, bool append = false, Encoding? encoding = null)
    {
        return WriteToFileAsync(path, content, append, encoding).GetAwaiter().GetResult();
    }

    public static NullOperationResult WriteToFile(string path, string[] content, bool append = false, Encoding? encoding = null)
    {
        return WriteToFileAsync(path, content, append, encoding).GetAwaiter().GetResult();
    }

    public static OperationResult<string[]> ReadFile(string path)
    {
        return ReadFileAsync(path).GetAwaiter().GetResult();
    }

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

    public static bool DoesFileExist(string path)
    {
        return File.Exists(path);
    }
    #endregion
}
