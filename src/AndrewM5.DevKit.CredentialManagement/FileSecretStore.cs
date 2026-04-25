using AndrewM5.DevKit.Core;
using AndrewM5.DevKit.Core.Results;
using Microsoft.AspNetCore.DataProtection;
using System.Text.Json;

namespace AndrewM5.DevKit.CredentialManagement;

/// <summary>
/// A file-based implementation of <see cref="SecretStoreBase"/> that persists encrypted JSON 
/// dictionaries to the local file system.
/// </summary>
/// <remarks>
/// This store organizes secrets into ".secret" files. Each file contains a dictionary of keys and values,
/// all of which are encrypted as a single block using the underlying <see cref="SecretStoreBase.Encrypt(string)"/> logic.
/// </remarks>
public class FileSecretStore : SecretStoreBase
{
    private readonly string _secretsDir;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSecretStore"/> class.
    /// </summary>
    /// <param name="provider">The <see cref="IDataProtectionProvider"/> used for cryptographic operations.</param>
    /// <param name="applicationName">The name of the application, used to identify the store and isolate its data.</param>
    /// <param name="secretsDir">The root directory path where the encrypted secret files will be saved.</param>
    public FileSecretStore(IDataProtectionProvider provider, string applicationName, string secretsDir) : base(provider, "FileSecretStore", applicationName)
    {
        _secretsDir = secretsDir;
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

            var writeToFile = FileExtension.WriteToFile(tmpFilePath, encrypted);
            if (!writeToFile.MethodSuccess)
            {
                throw writeToFile.Exception;
            }

            var moveFile = FileExtension.MoveFile(tmpFilePath, filePath, true);
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
    }

    /// <inheritdoc />
    /// <exception cref="KeyNotFoundException">Thrown if the specified <paramref name="key"/> does not exist within the file.</exception>
    public override OperationResult<string> GetKey(string fileName, string key)
    {
        var result = new OperationResult<string>();

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
            return result.SetMethodFailure(ex);
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

        try
        {
            var getPath = GetFilePath(fileName);
            if (!getPath.MethodSuccess)
            {
                throw getPath.Exception;
            }

            var filePath = getPath.Result;

            if (!FileExtension.DoesFileExist(filePath))
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

            var writeToFile = FileExtension.WriteToFile(tmpFilePath, encrypted);
            if (!writeToFile.MethodSuccess)
            {
                throw writeToFile.Exception;
            }

            var moveFile = FileExtension.MoveFile(tmpFilePath, filePath, true);
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
    }

    /// <inheritdoc />
    public override NullOperationResult DeleteSecret(string fileName)
    {
        var result = new NullOperationResult();

        try
        {
            var getPath = GetFilePath(fileName);
            if (!getPath.MethodSuccess)
            {
                throw getPath.Exception;
            }

            if (FileExtension.DoesFileExist(getPath.Result))
            {
                var deleteFile = FileExtension.DeleteFile(getPath.Result);
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
            if (!FileExtension.DoesFileExist(path))
            {
                return result.SetMethodSuccess(new Dictionary<string, object>());
            }

            var getContent = FileExtension.ReadFileText(path);
            if (!getContent.MethodSuccess)
            {
                throw getContent.Exception;
            }

            var json = Decrypt(getContent.Result);

            object? parsedObj = JsonUtils.ConvertJsonToObject(json);

            if (parsedObj is not Dictionary<string, object> parsedDictionary)
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
