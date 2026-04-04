using AndrewM5.DevKit.Core;
using AndrewM5.DevKit.Core.Results;
using Microsoft.AspNetCore.DataProtection;
using System.Text.Json;

namespace AndrewM5.DevKit.CredentialManagement;

/// <summary>
/// A file-based implementation of <see cref="ISecretStore"/> that stores encrypted JSON 
/// dictionaries on the local file system.
/// </summary>
public class FileSecretStore : SecretStoreBase
{
    private readonly string _secretsDir;

    /// <summary>
    /// Initializes a new instance of the <see cref="FileSecretStore"/> class.
    /// </summary>
    /// <param name="provider">The data protection provider for encryption.</param>
    /// <param name="applicationName">The name of the application, used as part of the store identity.</param>
    /// <param name="secretsDir">The directory path where secret files will be persisted.</param>
    public FileSecretStore(IDataProtectionProvider provider, string applicationName, string secretsDir) : base(provider, "FileSecretStore", applicationName)
    {
        _secretsDir = secretsDir;
    }

    /// <inheritdoc />
    /// <remarks>
    /// This method performs an atomic-style write by updating a temporary file and moving it 
    /// to the final destination to prevent data loss during power failure or crashes.
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
    /// Loads the encrypted file from disk, decrypts it, and deserializes the JSON content into a dictionary.
    /// </summary>
    /// <param name="path">The full path to the secret file.</param>
    /// <returns>An <see cref="OperationResult{T}"/> containing the decrypted key-value pairs.</returns>
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
    /// Generates a sanitized file path based on the provided <paramref name="keyName"/>.
    /// </summary>
    /// <param name="keyName">The name of the secret file/container.</param>
    /// <returns>An <see cref="OperationResult{string}"/> containing the safe absolute path.</returns>
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
    /// Helper method to extract a string value from various object types, 
    /// specifically handling <see cref="JsonElement"/> variations during deserialization.
    /// </summary>
    /// <param name="value">The object to convert.</param>
    /// <returns>The string representation of the object.</returns>
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
