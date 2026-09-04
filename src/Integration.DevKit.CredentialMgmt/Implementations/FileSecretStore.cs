/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.Core;
using Integration.DevKit.Core.Logging;
using Integration.DevKit.CredentialMgmt.Abstractions;
using Integration.DevKit.ThreadLocks;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace Integration.DevKit.CredentialMgmt.Implementations;

/// <summary>
/// A file-based implementation of <see cref="SecretStoreBase"/> that persists encrypted JSON
/// dictionaries to the local file system.
/// </summary>
/// <remarks>
/// This store organizes secrets into ".secret" files. Each file contains a dictionary of keys and values,
/// all of which are encrypted as a single block using the underlying <see cref="SecretStoreBase.Encrypt(string)"/> logic.
/// <para/>
/// Every public method locks around its full read-modify-write sequence via <see cref="ThreadLockManager"/>,
/// keyed per container (<c>fileName</c>). Without this, two concurrent calls against the same container could
/// both read the same starting state and one would silently overwrite the other's change on save.
/// </remarks>
public class FileSecretStore : SecretStoreBase
{
    private const int LockTimeoutMs = 5000;

    private readonly string _secretsDir;
    private readonly ThreadLockManager _threadLockManager;
    private readonly ILogger? _logger;

    /// <summary>
    /// Gets or sets whether this store logs through the logger factory supplied at construction.
    /// Defaults to <see langword="true"/>. Can be flipped at runtime to silence/resume this store's
    /// logging without removing the app's logger.
    /// </summary>
    public bool EnableLogging { get; set; } = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSecretStore"/> class.
    /// </summary>
    /// <param name="provider">The <see cref="IDataProtectionProvider"/> used for cryptographic operations.</param>
    /// <param name="applicationName">The name of the application, used to identify the store and isolate its data.</param>
    /// <param name="secretsDir">The root directory path where the encrypted secret files will be saved.</param>
    /// <param name="threadLockManager">
    /// Used to serialize concurrent access to the same container's underlying file across all of
    /// <see cref="SetKey"/>/<see cref="GetKey"/>/<see cref="DeleteKey"/>/<see cref="DeleteSecret"/>.
    /// </param>
    /// <param name="loggerFactory">Optional factory to resolve the "FileSecretStore" logger.</param>
    /// <param name="enableLogging">The initial value of <see cref="EnableLogging"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="threadLockManager"/> is null.</exception>
    public FileSecretStore(
        IDataProtectionProvider provider,
        string applicationName,
        string secretsDir,
        ThreadLockManager threadLockManager,
        ILoggerFactory? loggerFactory = null,
        bool enableLogging = true) : base(provider, "FileSecretStore", applicationName)
    {
        _secretsDir = secretsDir;
        _threadLockManager = threadLockManager ?? throw new ArgumentNullException(nameof(threadLockManager));

        EnableLogging = enableLogging;
        _logger = loggerFactory?.CreateConditionalLogger("FileSecretStore", () => EnableLogging);
    }

    /// <inheritdoc />
    /// <remarks>
    /// <b>Atomic Write Pattern:</b> To ensure data integrity, this method writes to a temporary file 
    /// (<c>.tmp</c>) first. Once the write is successful, it replaces the original file. This prevents 
    /// partial writes or file corruption in the event of an application crash or power loss.
    /// </remarks>
    public override NullOperationResult SetKey(string fileName, string key, string value)
    {
        var result = new NullOperationResult();
        var lockKey = $"{StoreName}:{fileName}";

        var enterLock = _threadLockManager.TryEnterSyncLock(lockKey, LockTimeoutMs);
        if (!enterLock.MethodSuccess)
        {
            _logger?.LogWarning($"Timed out waiting for lock '{lockKey}' while setting key '{key}'.");

            return result.SetMethodFailure(enterLock.Exception);
        }

        try
        {
            var getPath = GetFilePath(fileName);
            if (!getPath.MethodSuccess)
            {
                throw getPath.Exception;
            }

            var filePath = getPath.Result;
            var tmpFilePath = filePath + ".tmp";

            var getDecryptedContent = Load(filePath);
            if (!getDecryptedContent.MethodSuccess)
            {
                throw getDecryptedContent.Exception;
            }

            getDecryptedContent.Result[key] = value;

            var getJson = JsonUtils.SerializeObjectToJson(getDecryptedContent.Result);
            if (!getJson.MethodSuccess)
            {
                throw getJson.Exception;
            }

            var encrypted = Encrypt(getJson.Result);

            var writeToFile = FileUtils.WriteToFile(tmpFilePath, encrypted);
            if (!writeToFile.MethodSuccess)
            {
                throw writeToFile.Exception;
            }

            var moveFile = FileUtils.MoveFile(tmpFilePath, filePath, true);
            if (!moveFile.MethodSuccess)
            {
                throw moveFile.Exception;
            }

            _logger?.LogDebug($"Set key '{key}' in secret '{fileName}'.");

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Failed to set key '{key}' in secret '{fileName}'.");

            return result.SetMethodFailure(ex);
        }
        finally
        {
            _threadLockManager.TryExitSyncLock(lockKey);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// If the specified <paramref name="key"/> does not exist within the file, a failed
    /// <see cref="OperationResult{T}"/> wrapping a <see cref="KeyNotFoundException"/> is returned
    /// rather than thrown.
    /// </remarks>
    public override OperationResult<string> GetKey(string fileName, string key)
    {
        var result = new OperationResult<string>();
        var lockKey = $"{StoreName}:{fileName}";

        var enterLock = _threadLockManager.TryEnterSyncLock(lockKey, LockTimeoutMs);
        if (!enterLock.MethodSuccess)
        {
            return result.SetMethodFailure(enterLock.Exception);
        }

        try
        {
            var getPath = GetFilePath(fileName);
            if (!getPath.MethodSuccess)
            {
                throw getPath.Exception;
            }

            var getDecryptedContent = Load(getPath.Result);
            if (!getDecryptedContent.MethodSuccess)
            {
                throw getDecryptedContent.Exception;
            }

            if (!getDecryptedContent.Result.TryGetValue(key, out var raw))
            {
                throw new KeyNotFoundException($"Secret '{key}' not found");
            }

            var value = GetString(raw);

            return result.SetMethodSuccess(value);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, $"Failed to get key '{key}' from secret '{fileName}'.");

            return result.SetMethodFailure(ex);
        }
        finally
        {
            _threadLockManager.TryExitSyncLock(lockKey);
        }
    }

    /// <inheritdoc />
    /// <remarks>
    /// This method is idempotent. If the file or the key does not exist, it returns a successful result.
    /// Changes are committed using the atomic write pattern.
    /// </remarks>
    public override NullOperationResult DeleteKey(string fileName, string key)
    {
        var result = new NullOperationResult();
        var lockKey = $"{StoreName}:{fileName}";

        var enterLock = _threadLockManager.TryEnterSyncLock(lockKey, LockTimeoutMs);
        if (!enterLock.MethodSuccess)
        {
            return result.SetMethodFailure(enterLock.Exception);
        }

        try
        {
            var getPath = GetFilePath(fileName);
            if (!getPath.MethodSuccess)
            {
                throw getPath.Exception;
            }

            var filePath = getPath.Result;

            if (!FileUtils.DoesFileExist(filePath))
            {
                // Nothing to delete — treat as success
                return result.SetMethodSuccess();
            }

            var getDecryptedContent = Load(filePath);
            if (!getDecryptedContent.MethodSuccess)
            {
                throw getDecryptedContent.Exception;
            }

            if (!getDecryptedContent.Result.Remove(key))
            {
                // Key not found — your design choice:
                // Either treat as success or failure.
                // I recommend success for idempotency.
                return result.SetMethodSuccess();
            }

            var tmpFilePath = filePath + ".tmp";

            var getJson = JsonUtils.SerializeObjectToJson(getDecryptedContent.Result);
            if (!getJson.MethodSuccess)
            {
                throw getJson.Exception;
            }

            var encrypted = Encrypt(getJson.Result);

            var writeToFile = FileUtils.WriteToFile(tmpFilePath, encrypted);
            if (!writeToFile.MethodSuccess)
            {
                throw writeToFile.Exception;
            }

            var moveFile = FileUtils.MoveFile(tmpFilePath, filePath, true);
            if (!moveFile.MethodSuccess)
            {
                throw moveFile.Exception;
            }

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
        finally
        {
            _threadLockManager.TryExitSyncLock(lockKey);
        }
    }

    /// <inheritdoc />
    public override NullOperationResult DeleteSecret(string fileName)
    {
        var result = new NullOperationResult();
        var lockKey = $"{StoreName}:{fileName}";

        var enterLock = _threadLockManager.TryEnterSyncLock(lockKey, LockTimeoutMs);
        if (!enterLock.MethodSuccess)
        {
            return result.SetMethodFailure(enterLock.Exception);
        }

        try
        {
            var getPath = GetFilePath(fileName);
            if (!getPath.MethodSuccess)
            {
                throw getPath.Exception;
            }

            if (FileUtils.DoesFileExist(getPath.Result))
            {
                var deleteFile = FileUtils.DeleteFile(getPath.Result);
                if (!deleteFile.MethodSuccess)
                {
                    throw deleteFile.Exception;
                }
            }

            return result.SetMethodSuccess();
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
        finally
        {
            _threadLockManager.TryExitSyncLock(lockKey);
        }
    }

    /// <summary>
    /// Loads the encrypted file from disk, decrypts it, and deserializes the JSON content.
    /// </summary>
    /// <param name="path">The full path to the secret file.</param>
    /// <returns>A result containing a dictionary of secret keys and values. Returns an empty dictionary if the file does not exist.</returns>
    private OperationResult<Dictionary<string, object>> Load(string path)
    {
        var result = new OperationResult<Dictionary<string, object>>();

        try
        {
            if (!FileUtils.DoesFileExist(path))
            {
                return result.SetMethodSuccess(new Dictionary<string, object>());
            }

            var getContent = FileUtils.ReadFileText(path);
            if (!getContent.MethodSuccess)
            {
                throw getContent.Exception;
            }

            var json = Decrypt(getContent.Result);

            var getParsedObj = JsonUtils.DeserializeJsonToObject(json);
            if (!getParsedObj.MethodSuccess)
            {
                throw getParsedObj.Exception;
            }

            if (getParsedObj.Result is not Dictionary<string, object> parsedDictionary)
            {
                throw new Exception("Parsed object is not a Dictionary<string, object>");
            }

            return result.SetMethodSuccess(parsedDictionary);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Generates a sanitized file path by replacing invalid characters with underscores.
    /// </summary>
    /// <param name="keyName">The requested name for the secret container.</param>
    /// <returns>The sanitized absolute path to the file.</returns>
    private OperationResult<string> GetFilePath(string keyName)
    {
        var result = new OperationResult<string>();

        try
        {
            foreach (var c in Path.GetInvalidFileNameChars())
            {
                keyName = keyName.Replace(c, '_');
            }

            string path = Path.Combine(_secretsDir, $"{StoreName}_{keyName}.secret");

            return result.SetMethodSuccess(path);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }

    /// <summary>
    /// Handles extraction of strings from various types, including <see cref="JsonElement"/> 
    /// types returned by the JSON deserializer.
    /// </summary>
    private static string GetString(object value)
    {
        return value switch
        {
            string s => s,
            JsonElement je when je.ValueKind == JsonValueKind.String => je.GetString()!,
            JsonElement je => je.ToString(),
            _ => value.ToString()!
        };
    }
}
