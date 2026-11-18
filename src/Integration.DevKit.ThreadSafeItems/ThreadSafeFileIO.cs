/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.Core;
using Integration.DevKit.Core.Logging;
using Integration.DevKit.ThreadLocks;
using Integration.DevKit.ThreadSafeItems.Settings;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Text;

namespace Integration.DevKit.ThreadSafeItems;

/// <summary>
/// Provides thread-safe file I/O operations by utilizing <see cref="ThreadLockManager"/>
/// to synchronize access based on file paths.
/// </summary>
public class ThreadSafeFileIO
{
    /// <summary>
    /// Gets or sets the current runtime settings for this instance, initialized from the bound
    /// <see cref="ThreadSafeItemsSettings"/>. Mutate this in place (e.g. <c>RuntimeSettings.EnableLogging = false</c>)
    /// to change behavior, including logging, at runtime.
    /// </summary>
    public ThreadSafeItemsSettings RuntimeSettings { get; set; }

    private ThreadLockManager _threadLockManager;
    private ILogger? _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="ThreadSafeFileIO"/> class.
    /// </summary>
    /// <param name="threadLockManager">The manager used to handle synchronization locks.</param>
    /// <param name="settings">The initial configuration settings injected via the Options pattern.</param>
    /// <param name="loggerFactory">The factory used to resolve the internal logger.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="threadLockManager"/> is <see langword="null"/>.</exception>
    public ThreadSafeFileIO(ThreadLockManager threadLockManager, IOptions<ThreadSafeItemsSettings>? settings = null, ILoggerFactory? loggerFactory = null)
    {
        if (threadLockManager == null)
        {
            throw new ArgumentNullException(nameof(threadLockManager));
        }

        _threadLockManager = threadLockManager;
        RuntimeSettings = settings?.Value.Clone() ?? new ThreadSafeItemsSettings();
        _logger = loggerFactory?.CreateConditionalLogger(nameof(ThreadSafeFileIO), () => RuntimeSettings.EnableLogging);
    }

    #region Async Methods
    /// <summary>
    /// Asynchronously writes a string to a file with thread-safe locking.
    /// </summary>
    /// <param name="path">The full path to the file.</param>
    /// <param name="content">The string content to write.</param>
    /// <param name="append"><see langword="true"/> to append data; <see langword="false"/> to overwrite.</param>
    /// <param name="allowNoFileExtension">
    /// Optional flag to allow file names without an extension to pass validation 
    /// instead of requiring an explicit extension (e.g., '.txt').
    /// </param>
    /// <param name="encoding">The character encoding to use. Defaults to UTF-8 if null.</param>
    /// <param name="lockTimeoutMs">Maximum time in milliseconds to wait for the file lock.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating the outcome of the operation.</returns>
    public Task<NullOperationResult> WriteToFileAsync(string path, string content, bool append = false, bool allowNoFileExtension = false, Encoding? encoding = null, int lockTimeoutMs = 5000)
    {
        return WithLockAsync(path, lockTimeoutMs, () => FileUtils.WriteToFileAsync(path, content, append, allowNoFileExtension, encoding), ex => new NullOperationResult().SetMethodFailure(ex));
    }

    /// <summary>
    /// Asynchronously writes an array of strings (lines) to a file with thread-safe locking.
    /// </summary>
    /// <param name="path">The full path to the file. This path is used as the unique lock key.</param>
    /// <param name="content">The array of strings to write as lines.</param>
    /// <param name="append"><see langword="true"/> to append data; <see langword="false"/> to overwrite the existing file.</param>
    /// <param name="allowNoFileExtension">
    /// Optional flag to allow file names without an extension to pass validation 
    /// instead of requiring an explicit extension (e.g., '.txt').
    /// </param>
    /// <param name="encoding">The character encoding to use. Defaults to UTF-8 if <see langword="null"/>.</param>
    /// <param name="lockTimeoutMs">The maximum time in milliseconds to wait for the file lock. Defaults to 5000ms.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating the outcome of the operation.</returns>
    public Task<NullOperationResult> WriteToFileAsync(string path, string[] content, bool append = false, bool allowNoFileExtension = false, Encoding? encoding = null, int lockTimeoutMs = 5000)
    {
        return WithLockAsync(path, lockTimeoutMs, () => FileUtils.WriteToFileAsync(path, content, append, allowNoFileExtension, encoding), ex => new NullOperationResult().SetMethodFailure(ex));
    }

    /// <summary>
    /// Asynchronously reads all lines from a file with thread-safe locking.
    /// </summary>
    /// <param name="path">The full path to the file.</param>
    /// <param name="lockTimeoutMs">The maximum time in milliseconds to wait for the file lock. Defaults to 5000ms.</param>
    /// <param name="allowNoFileExtension">Allows file names without an extension (e.g. 'LICENSE').</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the array of lines read from the file.</returns>
    public Task<OperationResult<string[]>> ReadFileLinesAsync(string path, int lockTimeoutMs = 5000, bool allowNoFileExtension = false)
    {
        return WithLockAsync(path, lockTimeoutMs, () => FileUtils.ReadFileLinesAsync(path, allowNoFileExtension), ex => new OperationResult<string[]>().SetMethodFailure(ex));
    }

    /// <summary>
    /// Asynchronously reads the entire content of a file as a single string with thread-safe locking.
    /// </summary>
    /// <param name="path">The full path to the file.</param>
    /// <param name="lockTimeoutMs">The maximum time in milliseconds to wait for the file lock. Defaults to 5000ms.</param>
    /// <param name="allowNoFileExtension">Allows file names without an extension (e.g. 'LICENSE').</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the file text.</returns>
    public Task<OperationResult<string>> ReadFileTextAsync(string path, int lockTimeoutMs = 5000, bool allowNoFileExtension = false)
    {
        return WithLockAsync(path, lockTimeoutMs, () => FileUtils.ReadFileTextAsync(path, allowNoFileExtension), ex => new OperationResult<string>().SetMethodFailure(ex));
    }
    #endregion

    #region Sync Methods
    /// <summary>
    /// Synchronously writes a string to a file with thread-safe locking.
    /// </summary>
    /// <param name="path">The full path to the file.</param>
    /// <param name="content">The string content to write.</param>
    /// <param name="append"><see langword="true"/> to append data; <see langword="false"/> to overwrite.</param>
    /// <param name="allowNoFileExtension">
    /// Optional flag to allow file names without an extension to pass validation 
    /// instead of requiring an explicit extension (e.g., '.txt').
    /// </param>
    /// <param name="encoding">The character encoding to use. Defaults to UTF-8 if <see langword="null"/>.</param>
    /// <param name="lockTimeoutMs">The maximum time in milliseconds to wait for the file lock. Defaults to 5000ms.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating the outcome of the operation.</returns>
    public NullOperationResult WriteToFile(string path, string content, bool append = false, bool allowNoFileExtension = false, Encoding? encoding = null, int lockTimeoutMs = 5000)
    {
        return WithLockSync(path, lockTimeoutMs, () => FileUtils.WriteToFile(path, content, append, allowNoFileExtension, encoding), ex => new NullOperationResult().SetMethodFailure(ex));
    }

    /// <summary>
    /// Synchronously writes an array of strings (lines) to a file with thread-safe locking.
    /// </summary>
    /// <param name="path">The full path to the file.</param>
    /// <param name="content">The array of strings to write.</param>
    /// <param name="append"><see langword="true"/> to append data; <see langword="false"/> to overwrite.</param>
    /// <param name="allowNoFileExtension">
    /// Optional flag to allow file names without an extension to pass validation 
    /// instead of requiring an explicit extension (e.g., '.txt').
    /// </param>
    /// <param name="encoding">The character encoding to use. Defaults to UTF-8 if <see langword="null"/>.</param>
    /// <param name="lockTimeoutMs">The maximum time in milliseconds to wait for the file lock. Defaults to 5000ms.</param>
    /// <returns>A <see cref="NullOperationResult"/> indicating the outcome of the operation.</returns>
    public NullOperationResult WriteToFile(string path, string[] content, bool append = false, bool allowNoFileExtension = false, Encoding? encoding = null, int lockTimeoutMs = 5000)
    {
        return WithLockSync(path, lockTimeoutMs, () => FileUtils.WriteToFile(path, content, append, allowNoFileExtension, encoding), ex => new NullOperationResult().SetMethodFailure(ex));
    }

    /// <summary>
    /// Synchronously reads all lines from a file with thread-safe locking.
    /// </summary>
    /// <param name="path">The full path to the file.</param>
    /// <param name="lockTimeoutMs">The maximum time in milliseconds to wait for the file lock. Defaults to 5000ms.</param>
    /// <param name="allowNoFileExtension">Allows file names without an extension (e.g. 'LICENSE').</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the array of lines read from the file.</returns>
    public OperationResult<string[]> ReadFileLines(string path, int lockTimeoutMs = 5000, bool allowNoFileExtension = false)
    {
        return WithLockSync(path, lockTimeoutMs, () => FileUtils.ReadFileLines(path, allowNoFileExtension), ex => new OperationResult<string[]>().SetMethodFailure(ex));
    }

    /// <summary>
    /// Synchronously reads the entire content of a file as a string with thread-safe locking.
    /// </summary>
    /// <param name="path">The full path to the file.</param>
    /// <param name="lockTimeoutMs">The maximum time in milliseconds to wait for the file lock. Defaults to 5000ms.</param>
    /// <param name="allowNoFileExtension">Allows file names without an extension (e.g. 'LICENSE').</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the file text.</returns>
    public OperationResult<string> ReadFileText(string path, int lockTimeoutMs = 5000, bool allowNoFileExtension = false)
    {
        return WithLockSync(path, lockTimeoutMs, () => FileUtils.ReadFileText(path, allowNoFileExtension), ex => new OperationResult<string>().SetMethodFailure(ex));
    }
    #endregion

    /// <summary>
    /// Runs <paramref name="work"/> while holding the async lock for <paramref name="path"/>.
    /// The lock is only released if it was acquired, so a timeout never frees a lock owned by another caller.
    /// </summary>
    private async Task<T> WithLockAsync<T>(string path, int lockTimeoutMs, Func<Task<T>> work, Func<Exception, T> failure)
    {
        string lockKey = ToLockKey(path);
        var lockResult = await _threadLockManager.TryEnterAsyncLock(lockKey, lockTimeoutMs);
        if (!lockResult.MethodSuccess)
        {
            _logger?.LogError(lockResult.Exception, $"File operation failed for '{path}'.");

            return failure(lockResult.Exception);
        }

        try
        {
            return await work();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"File operation failed for '{path}'.");

            return failure(ex);
        }
        finally
        {
            var exitLock = _threadLockManager.TryExitAsyncLock(lockKey);
            if (!exitLock.MethodSuccess)
            {
                _logger?.LogError(exitLock.Exception, $"Failed to release lock for '{path}'.");
            }
        }
    }

    /// <summary>
    /// Runs <paramref name="work"/> while holding the sync lock for <paramref name="path"/>.
    /// The lock is only released if it was acquired.
    /// </summary>
    private T WithLockSync<T>(string path, int lockTimeoutMs, Func<T> work, Func<Exception, T> failure)
    {
        string lockKey = ToLockKey(path);
        var lockResult = _threadLockManager.TryEnterSyncLock(lockKey, lockTimeoutMs);
        if (!lockResult.MethodSuccess)
        {
            _logger?.LogError(lockResult.Exception, $"File operation failed for '{path}'.");

            return failure(lockResult.Exception);
        }

        try
        {
            return work();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"File operation failed for '{path}'.");

            return failure(ex);
        }
        finally
        {
            var exitLock = _threadLockManager.TryExitSyncLock(lockKey);
            if (!exitLock.MethodSuccess)
            {
                _logger?.LogError(exitLock.Exception, $"Failed to release lock for '{path}'.");
            }
        }
    }

    /// <summary>
    /// Gets the lock key for <paramref name="path"/>: its full path, so <c>data\out.txt</c>, <c>.\data\out.txt</c>
    /// and <c>C:\app\data\out.txt</c> share one lock instead of each getting their own.
    /// </summary>
    private static string ToLockKey(string path)
    {
        try
        {
            return Path.GetFullPath(path);
        }
        catch
        {
            // Not a usable path: the lock manager or the file call reports the error
            if (path == null)
            {
                return "";
            }

            return path;
        }
    }
}
