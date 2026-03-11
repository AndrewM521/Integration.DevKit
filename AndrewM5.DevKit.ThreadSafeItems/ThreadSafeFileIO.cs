using AndrewM5.DevKit.Core;
using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.Logging.Abstractions;
using AndrewM5.DevKit.ThreadLocks.Abstractions;
using Microsoft.Extensions.Logging;
using System.Text;

namespace AndrewM5.DevKit.ThreadSafeItems;

public class ThreadSafeFileIO
{
    private IThreadLockManager _threadLockManager;
    private ICustomLogger _logger;

    public ThreadSafeFileIO(IThreadLockManager threadLockManager, ICustomLoggerManager loggerManager)
    {
        if (threadLockManager == null)
        {
            throw new ArgumentNullException(nameof(threadLockManager));
        }

        if (loggerManager == null)
        {
            throw new ArgumentNullException(nameof(loggerManager));
        }

        _threadLockManager = threadLockManager;
        _logger = loggerManager.GetLogger("ThreadSafeFileIO");
    }

    #region Async Methods
    public async Task<NullOperationResult> WriteToFileAsync(string path, string content, bool append = false, Encoding? encoding = null, int lockTimeoutMs = 5000)
    {
        var result = new NullOperationResult();

        try
        {
            var lockResult = await _threadLockManager.TryEnterAsyncLock(path, lockTimeoutMs);
            if (!lockResult.MethodSuccess)
            {
                throw lockResult.Exception;
            }

            return await FileExtension.WriteToFileAsync(path, content, append, encoding);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex.Message);

            return result.SetMethodFailure(ex);
        }
        finally
        {
            var exitLock = _threadLockManager.TryExitAsyncLock(path);
            if (!exitLock.MethodSuccess)
            {
                _logger?.LogError(exitLock.Exception.Message);
            }
        }
    }

    public async Task<NullOperationResult> WriteToFileAsync(string path, string[] content, bool append = false, Encoding? encoding = null, int lockTimeoutMs = 5000)
    {
        var result = new NullOperationResult();

        try
        {
            var lockResult = await _threadLockManager.TryEnterAsyncLock(path, lockTimeoutMs);
            if (!lockResult.MethodSuccess)
            {
                throw lockResult.Exception;
            }

            return await FileExtension.WriteToFileAsync(path, content, append, encoding);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex.Message);

            return result.SetMethodFailure(ex);
        }
        finally
        {
            var exitLock = _threadLockManager.TryExitAsyncLock(path);
            if (!exitLock.MethodSuccess)
            {
                _logger?.LogError(exitLock.Exception.Message);
            }
        }
    }

    public async Task<OperationResult<string[]>> ReadFileLinesAsync(string path, int lockTimeoutMs = 5000)
    {
        var result = new OperationResult<string[]>();

        try
        {
            var lockResult = await _threadLockManager.TryEnterAsyncLock(path, lockTimeoutMs);
            if (!lockResult.MethodSuccess)
            {
                throw lockResult.Exception;
            }

            var readResult = await FileExtension.ReadFileLinesAsync(path);
            if (!readResult.MethodSuccess)
            {
                throw readResult.Exception;
            }

            return result.SetMethodSuccess(readResult.Result);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex.Message);

            return result.SetMethodFailure(ex);
        }
        finally
        {
            var exitLock = _threadLockManager.TryExitAsyncLock(path);
            if (!exitLock.MethodSuccess)
            {
                _logger?.LogError(exitLock.Exception.Message);
            }
        }
    }

    public async Task<OperationResult<string>> ReadFileTextAsync(string path, int lockTimeoutMs = 5000)
    {
        var result = new OperationResult<string>();

        try
        {
            var lockResult = await _threadLockManager.TryEnterAsyncLock(path, lockTimeoutMs);
            if (!lockResult.MethodSuccess)
            {
                throw lockResult.Exception;
            }

            var readResult = await FileExtension.ReadFileTextAsync(path);
            if (!readResult.MethodSuccess)
            {
                throw readResult.Exception;
            }

            return result.SetMethodSuccess(readResult.Result);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex.Message);

            return result.SetMethodFailure(ex);
        }
        finally
        {
            var exitLock = _threadLockManager.TryExitAsyncLock(path);
            if (!exitLock.MethodSuccess)
            {
                _logger?.LogError(exitLock.Exception.Message);
            }
        }
    }
    #endregion

    #region Sync Methods
    public NullOperationResult WriteToFile(string path, string content, bool append = false, Encoding? encoding = null, int lockTimeoutMs = 5000)
    {
        var result = new NullOperationResult();

        try
        {
            var lockResult = _threadLockManager.TryEnterSyncLock(path, lockTimeoutMs);
            if (!lockResult.MethodSuccess)
            {
                throw lockResult.Exception;
            }

            return FileExtension.WriteToFile(path, content, append, encoding);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex.Message);

            return result.SetMethodFailure(ex);
        }
        finally
        {
            var exitLock = _threadLockManager.TryExitSyncLock(path);
            if (!exitLock.MethodSuccess)
            {
                _logger?.LogError(exitLock.Exception.Message);
            }
        }
    }

    public NullOperationResult WriteToFile(string path, string[] content, bool append = false, Encoding? encoding = null, int lockTimeoutMs = 5000)
    {
        var result = new NullOperationResult();

        try
        {
            var lockResult = _threadLockManager.TryEnterSyncLock(path, lockTimeoutMs);
            if (!lockResult.MethodSuccess)
            {
                throw lockResult.Exception;
            }

            return FileExtension.WriteToFile(path, content, append, encoding);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex.Message);

            return result.SetMethodFailure(ex);
        }
        finally
        {
            var exitLock = _threadLockManager.TryExitSyncLock(path);
            if (!exitLock.MethodSuccess)
            {
                _logger?.LogError(exitLock.Exception.Message);
            }
        }
    }

    public OperationResult<string[]> ReadFileLines(string path, int lockTimeoutMs = 5000)
    {
        var result = new OperationResult<string[]>();

        try
        {
            var lockResult = _threadLockManager.TryEnterSyncLock(path, lockTimeoutMs);
            if (!lockResult.MethodSuccess)
            {
                throw lockResult.Exception;
            }

            var readResult = FileExtension.ReadFileLines(path);
            if (!readResult.MethodSuccess)
            {
                throw readResult.Exception;
            }

            return result.SetMethodSuccess(readResult.Result);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex.Message);

            return result.SetMethodFailure(ex);
        }
        finally
        {
            var exitLock = _threadLockManager.TryExitSyncLock(path);
            if (!exitLock.MethodSuccess)
            {
                _logger?.LogError(exitLock.Exception.Message);
            }
        }
    }

    public OperationResult<string> ReadFileText(string path, int lockTimeoutMs = 5000)
    {
        var result = new OperationResult<string>();

        try
        {
            var lockResult = _threadLockManager.TryEnterSyncLock(path, lockTimeoutMs);
            if (!lockResult.MethodSuccess)
            {
                throw lockResult.Exception;
            }

            var readResult = FileExtension.ReadFileText(path);
            if (!readResult.MethodSuccess)
            {
                throw readResult.Exception;
            }

            return result.SetMethodSuccess(readResult.Result);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex.Message);

            return result.SetMethodFailure(ex);
        }
        finally
        {
            var exitLock = _threadLockManager.TryExitSyncLock(path);
            if (!exitLock.MethodSuccess)
            {
                _logger?.LogError(exitLock.Exception.Message);
            }
        }
    }
    #endregion
}
