/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

namespace Integration.DevKit.Core;

/// <summary>
/// Utility methods for directory-level file system operations, 
/// including creation, deletion, validation, and sanitization.
/// </summary>
public static class DirectoryUtils
{
    private static readonly string RequiredDirectoryPathErrorMsg = "Path must be a Directory.";

    #region Main Methods
    /// <summary>
    /// Validates the provided path and creates the directory if it does not already exist.
    /// </summary>
    /// <param name="path">The full path of the directory to create.</param>
    /// <returns> A <see cref="NullOperationResult"/> indicating success or failure.</returns>
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
    /// <param name="recursive">
    /// <see langword="true"/> to remove directories, subdirectories, and files within the path; 
    /// <see langword="false"/> to delete only the directory if it is empty.
    /// </param>
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
    /// Retrieves the file paths that match a search pattern within a directory.
    /// </summary>
    /// <param name="path">The relative or absolute path to the directory to search.</param>
    /// <param name="searchOption">Optional search option to include all subdirectories or only the current directory. Defaults to <see cref="SearchOption.TopDirectoryOnly"/>.</param>
    /// <param name="searchPattern">Optional search string to match against file names (e.g., "*.txt", "data_??.json").</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing a string array of file paths.</returns>
    public static OperationResult<string[]> GetFiles(string path, SearchOption searchOption = SearchOption.TopDirectoryOnly, string searchPattern = "*")
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

            var files = Directory.GetFiles(path, searchPattern, searchOption);

            return result.SetMethodSuccess(files);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Performs a heuristic check to determine if a string is formatted as a valid directory path.
    /// </summary>
    /// <param name="path">The string path to validate.</param>
    /// <returns>
    /// An <see cref="OperationResult{T}"/> where the result is <see langword="true"/> if the string is structurally valid to be a directory.
    /// </returns>
    /// <remarks>
    /// This method does not check if the directory actually exists on disk; it only validates the string structure.
    /// </remarks>
    public static OperationResult<bool> IsStringValidDirectoryPath(string path)
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

            /// WHY WE USE FileInfo: Even though this validates a directory path, we instantiate a FileInfo object 
            /// to trick .NET into running the string through its strict, native OS-level path parsing engine. 
            /// This instantly catches complex structural errors (like malformed drive syntax "C:::\\" or path length 
            /// limits) without doing an expensive disk I/O check. We avoid DirectoryInfo here because it has a 
            /// known quirk where it automatically strips trailing dots/spaces during parsing, which masks formatting errors.
            var fileInfo = new FileInfo(path);

            var lastSegment = Path.GetFileName(path);
            if (!string.IsNullOrEmpty(lastSegment) && lastSegment.IndexOfAny(Path.GetInvalidFileNameChars()) != -1)
            {
                return result.SetMethodSuccess(false);
            }

            var trimmedPath = path.TrimEnd();

            // A valid directory intent means it explicitly ends with a slash OR it has no extension.
            // If BOTH are false, it heavily implies a file intent, so we reject it.
            bool hasDirectoryIntent = trimmedPath.EndsWith(Path.DirectorySeparatorChar) ||
                                      trimmedPath.EndsWith(Path.AltDirectorySeparatorChar) ||
                                      string.IsNullOrEmpty(fileInfo.Extension);
            if (!hasDirectoryIntent)
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
    /// Sanitizes a string for use as a directory name by replacing invalid characters.
    /// </summary>
    /// <param name="directoryName">The potential directory name to sanitize.</param>
    /// <param name="replacement">The character used to replace invalid file system characters. Defaults to '_'.</param>
    /// <returns>
    /// An <see cref="OperationResult{T}"/> containing the sanitized directory name.
    /// </returns>
    /// <remarks>
    /// This method removes trailing periods and spaces and replaces characters identified by 
    /// <see cref="Path.GetInvalidFileNameChars"/>.
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
    /// Determines whether the specified path refers to an existing directory on disk.
    /// </summary>
    /// <param name="path">The path to test.</param>
    /// <returns><see langword="true"/> if the directory exists; otherwise, <see langword="false"/>.</returns>
    public static bool DoesDirectoryExist(string path)
    {
        return Directory.Exists(path);
    }
    #endregion
}
