using AndrewM5.DevKit.Core.Results;

namespace AndrewM5.DevKit.Core;

public static class DirectoryExtension
{
    private static readonly string RequiredDirectoryPathErrorMsg = "Path must be a Directory.";

    #region Main Methods
    public static OperationResult<bool> CreateDirectory(string path)
    {
        var result = new OperationResult<bool>();

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

            return result.SetMethodSuccess(true);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }
    
    public static OperationResult<bool> DeleteDirectory(string path, bool recursive = false)
    {
        var result = new OperationResult<bool>();

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

            return result.SetMethodSuccess(true);
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

    public static bool DoesDirectoryExist(string path)
    {
        return Directory.Exists(path);
    }
    #endregion
}
