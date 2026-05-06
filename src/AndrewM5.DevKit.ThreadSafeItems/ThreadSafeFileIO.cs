/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using AndrewM5.DevKit.Core;
using AndrewM5.DevKit.Core.Results;
using AndrewM5.DevKit.CustomLogger.Contracts.Interfaces;
using AndrewM5.DevKit.ThreadLocks.Contracts.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text;

namespace AndrewM5.DevKit.ThreadSafeItems;

/// <summary>
/// Provides thread-safe file I/O operations by utilizing <see cref="IThreadLockManager"/> 
/// to synchronize access based on file paths.
/// </summary>
public class ThreadSafeFileIO
{
    private IThreadLockManager _threadLockManager;
    private ICustomLogger? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThreadSafeFileIO"/> class.
    /// </summary>
    /// <param name="threadLockManager">The manager used to handle synchronization locks.</param>
    /// <param name="loggerManager">The manager used to resolve the internal logger.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="threadLockManager"/> or <paramref name="loggerManager"/> is null.</exception>
    public ThreadSafeFileIO(IThreadLockManager threadLockManager, ICustomLoggerManager loggerManager)
    {
        if (threadLockManager == null)
        {
            throw new ArgumentNullException(nameof(threadLockManager));
        }

        _threadLockManager = threadLockManager;
        _logger = loggerManager?.GetLogger("ThreadSafeFileIO");
    }

    #region Async Methods
    /// <summary>
    /// Asynchronously writes a string to a file with thread-safe locking.
    /// </summary>
    /// <param name="path">The full path to the file.</param>
    /// <param name="content">The string content to write.</param>
    /// <param name="append"><see langword="true"/> to append data; <see langword="false"/> to overwrite.</param>
    /// <param name="encoding">The character encoding to use. Defaults to UTF-8 if null.</param>
    /// <param name="lockTimeoutMs">Maximum time in milliseconds to wait for the file lock.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating the outcome of the operation.</returns>
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

            return await FileUtils.WriteToFileAsync(path, content, append, encoding);
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

    /// <summary>
    /// Asynchronously writes an array of strings (lines) to a file with thread-safe locking.
    /// </summary>
    /// <param name="path">The full path to the file. This path is used as the unique lock key.</param>
    /// <param name="content">The array of strings to write as lines.</param>
    /// <param name="append"><see langword="true"/> to append data; <see langword="false"/> to overwrite the existing file.</param>
    /// <param name="encoding">The character encoding to use. Defaults to UTF-8 if <see langword="null"/>.</param>
    /// <param name="lockTimeoutMs">The maximum time in milliseconds to wait for the file lock. Defaults to 5000ms.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating the outcome of the operation.</returns>
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

            return await FileUtils.WriteToFileAsync(path, content, append, encoding);
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

    /// <summary>
    /// Asynchronously reads all lines from a file with thread-safe locking.
    /// </summary>
    /// <param name="path">The full path to the file.</param>
    /// <param name="lockTimeoutMs">The maximum time in milliseconds to wait for the file lock. Defaults to 5000ms.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the array of lines read from the file.</returns>
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

            var readResult = await FileUtils.ReadFileLinesAsync(path);
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

    /// <summary>
    /// Asynchronously reads the entire content of a file as a single string with thread-safe locking.
    /// </summary>
    /// <param name="path">The full path to the file.</param>
    /// <param name="lockTimeoutMs">The maximum time in milliseconds to wait for the file lock. Defaults to 5000ms.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the file text.</returns>
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

            var readResult = await FileUtils.ReadFileTextAsync(path);
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
    /// <summary>
    /// Synchronously writes a string to a file with thread-safe locking.
    /// </summary>
    /// <param name="path">The full path to the file.</param>
    /// <param name="content">The string content to write.</param>
    /// <param name="append"><see langword="true"/> to append data; <see langword="false"/> to overwrite.</param>
    /// <param name="encoding">The character encoding to use. Defaults to UTF-8 if <see langword="null"/>.</param>
    /// <param name="lockTimeoutMs">The maximum time in milliseconds to wait for the file lock. Defaults to 5000ms.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating the outcome of the operation.</returns>
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

            return FileUtils.WriteToFile(path, content, append, encoding);
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

    /// <summary>
    /// Synchronously writes an array of strings (lines) to a file with thread-safe locking.
    /// </summary>
    /// <param name="path">The full path to the file.</param>
    /// <param name="content">The array of strings to write.</param>
    /// <param name="append"><see langword="true"/> to append data; <see langword="false"/> to overwrite.</param>
    /// <param name="encoding">The character encoding to use. Defaults to UTF-8 if <see langword="null"/>.</param>
    /// <param name="lockTimeoutMs">The maximum time in milliseconds to wait for the file lock. Defaults to 5000ms.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating the outcome of the operation.</returns>
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

            return FileUtils.WriteToFile(path, content, append, encoding);
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

    /// <summary>
    /// Synchronously reads all lines from a file with thread-safe locking.
    /// </summary>
    /// <param name="path">The full path to the file.</param>
    /// <param name="lockTimeoutMs">The maximum time in milliseconds to wait for the file lock. Defaults to 5000ms.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the array of lines read from the file.</returns>
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

            var readResult = FileUtils.ReadFileLines(path);
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

    /// <summary>
    /// Synchronously reads the entire content of a file as a string with thread-safe locking.
    /// </summary>
    /// <param name="path">The full path to the file.</param>
    /// <param name="lockTimeoutMs">The maximum time in milliseconds to wait for the file lock. Defaults to 5000ms.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the file text.</returns>
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

            var readResult = FileUtils.ReadFileText(path);
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
