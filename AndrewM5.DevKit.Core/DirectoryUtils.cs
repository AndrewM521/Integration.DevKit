using AndrewM5.DevKit.Core.Results;

namespace AndrewM5.DevKit.Core;

public static class DirectoryUtils
{
    private static readonly string RequiredDirectoryPathErrorMsg = "Path must be a Directory.";

    #region Main Methods
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

    public static bool DoesDirectoryExist(string path)
    {
        return Directory.Exists(path);
    }
    #endregion
}
