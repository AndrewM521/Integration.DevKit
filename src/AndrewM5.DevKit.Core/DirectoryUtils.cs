using AndrewM5.DevKit.Core.Results;

namespace AndrewM5.DevKit.Core;

/// <summary>
/// Provides utility methods for directory-level file system operations.
/// </summary>
public static class DirectoryUtils
{
    private static readonly string RequiredDirectoryPathErrorMsg = "Path must be a Directory.";

    #region Main Methods
    /// <summary>
    /// Validates the path and creates the directory if it does not already exist.
    /// </summary>
    /// <param name="path">The full path of the directory to create.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating success or failure.</returns>
    /// <exception cref="ArgumentException">Thrown if the path is invalid or is not recognized as a directory.</exception>
    public static NullOperationResult CreateDirectory(string path)
    {
        var result = new NullOperationResult();

        try
        {
            var validatePath = IsStringValidDirectoryPath(path);
            if (!validatePath.MethodSuccess)
            {
                throw validatePath.Exception;
            }

            if (!validatePath.Result)
            {
                throw new ArgumentException(RequiredDirectoryPathErrorMsg);
            }

            if (!DoesDirectoryExist(path))
            {
                Directory.CreateDirectory(path);
            }

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Deletes the specified directory if it exists.
    /// </summary>
    /// <param name="path">The path of the directory to delete.</param>
    /// <param name="recursive"><c>true</c> to remove directories, subdirectories, and files in path; otherwise, <c>false</c>. Defaults to <c>false</c></param>
    /// <returns>A <see cref="NullOperationResult"/> indicating success or failure.</returns>
    public static NullOperationResult DeleteDirectory(string path, bool recursive = false)
    {
        var result = new NullOperationResult();

        try
        {
            var validatePath = IsStringValidDirectoryPath(path);
            if (!validatePath.MethodSuccess)
            {
                throw validatePath.Exception;
            }

            if (!validatePath.Result)
            {
                throw new ArgumentException(RequiredDirectoryPathErrorMsg);
            }

            if (DoesDirectoryExist(path))
            {
                Directory.Delete(path, recursive);
            }

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Returns the names of files (including their paths) that match the specified search pattern in the specified directory.
    /// </summary>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="searchPattern">The search string to match against the names of files in path (e.g., "*.txt").</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing an array of file paths.</returns>
    public static OperationResult<string[]> GetFiles(string path, string searchPattern)
    {
        var result = new OperationResult<string[]>();

        try
        {
            var validatePath = IsStringValidDirectoryPath(path);
            if (!validatePath.MethodSuccess)
            {
                throw validatePath.Exception;
            }

            if (!validatePath.Result)
            {
                throw new ArgumentException(RequiredDirectoryPathErrorMsg);
            }

            var files = Directory.GetFiles(path, searchPattern);

            return result.SetMethodSuccess(files);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Performs a heuristic check to determine if a string is a valid directory path format.
    /// </summary>
    /// <param name="path">The string path to validate.</param>
    /// <returns>
    /// An <see cref="OperationResult{T}"/> where the result is <c>true</c> if the path is valid and 
    /// appears to be a directory (ends in a separator or has no file extension).
    /// </returns>
    /// <remarks>
    /// This method checks for null/whitespace and invalid path characters before determining directory status.
    /// </remarks>
    public static OperationResult<bool> IsStringValidDirectoryPath(string path)
    {
        var result = new OperationResult<bool>();

        try
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("Path cannot be null or whitespace.");
            }

            if (path.IndexOfAny(Path.GetInvalidPathChars()) != -1)
            {
                throw new ArgumentException("Directory contains invalid characters.");
            }

            bool isDirectory = false;
            
            if (path.EndsWith("/") || path.EndsWith("\\") || !Path.HasExtension(path))
            {
                isDirectory = true;
            }

            return result.SetMethodSuccess(isDirectory);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Sanitizes a string to be used as a valid directory name by replacing invalid characters.
    /// </summary>
    /// <param name="directoryName">The potential directory name to sanitize.</param>
    /// <param name="replacement">The character used to replace invalid file system characters. Defaults to '_'.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the sanitized string.</returns>
    /// <remarks>
    /// Also trims trailing periods and spaces, which are invalid in many file systems.
    /// </remarks>
    public static OperationResult<string> GetSafeDirectoryName(string directoryName, char replacement = '_')
    {
        var result = new OperationResult<string>();

        try
        {
            if (string.IsNullOrWhiteSpace(directoryName))
            {
                throw new ArgumentException("Directory name cannot be null or whitespace.");
            }

            char[] invalidChars = Path.GetInvalidFileNameChars();
            char[] buffer = new char[directoryName.Length];

            for (int i = 0; i < directoryName.Length; i++)
            {
                char current = directoryName[i];
                bool isInvalid = false;

                for (int j = 0; j < invalidChars.Length; j++)
                {
                    if (current == invalidChars[j])
                    {
                        isInvalid = true;
                        break;
                    }
                }

                if (isInvalid)
                {
                    buffer[i] = replacement;
                }
                else
                {
                    buffer[i] = current;
                }
            }

            string sanitized = new string(buffer).TrimEnd('.', ' ');

            if (string.IsNullOrWhiteSpace(sanitized))
            {
                throw new ArgumentException("Directory name contains no valid characters.");
            }

            return result.SetMethodSuccess(sanitized);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Determines whether the given path refers to an existing directory.
    /// </summary>
    /// <param name="path">The path to test.</param>
    /// <returns><c>true</c> if the directory exists; otherwise, <c>false</c>.</returns>
    public static bool DoesDirectoryExist(string path)
    {
        return Directory.Exists(path);
    }
    #endregion
}
