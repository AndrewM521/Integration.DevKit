using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Integration.DevKit.Core.Configuration;

public sealed class JsonEncryptor
{
    private readonly CryptoContract _contract;
    private readonly EncryptionOptions _options;

    public JsonEncryptor(CryptoContract contract, EncryptionOptions options)
    {
        _options = options;
        _contract = contract;
    }

    public void Execute()
    {
        string fileName = Path.GetFullPath(_options.FileName);

        if (!FileUtils.DoesFileExist(fileName))
        {
            if (!_options.ThrowOnMissingFile)
            {
                throw new FileNotFoundException($"Configuration file not found: {fileName}");
            }

            Debug.WriteLine($"[EncryptConfig] File not found: {fileName}. Skipping encryption.");
            return;
        }

        var readAllText = FileUtils.ReadFileText(fileName);
        if (!readAllText.MethodSuccess)
        {
            throw readAllText.Exception;
        }

        var root = JsonNode.Parse(readAllText.Result,
            documentOptions: new JsonDocumentOptions { 
                CommentHandling = JsonCommentHandling.Skip
            }
        )!.AsObject();

        if (root is null)
        {
            throw new InvalidOperationException("appsettings.json is empty or invalid.");
        }

        foreach (var path in _options.Paths)
        {
            EncryptNode(root, path);
        }

        var getJson = JsonUtils.SerializeObjectToJson(root);
        if (!getJson.MethodSuccess)
        {
            throw getJson.Exception;
        }

        var writeToFile = FileUtils.WriteToFile(fileName, getJson.Result);
        if (!writeToFile.MethodSuccess)
        {
            throw writeToFile.Exception;
        }
    }

    private void EncryptNode(JsonObject root, string path)
    {
        var parts = path.Split(':');

        JsonNode? currentNode = root;

        foreach (var part in parts)
        {
            currentNode = currentNode?[part];

            if (currentNode is null)
                return;
        }

        switch (currentNode)
        {
            case JsonObject obj:
                EncryptNode(obj);
                break;

            case JsonValue value:
                EncryptValue(value);
                break;

            case JsonArray array:
                EncryptNode(array);
                break;
        }
    }

    private void EncryptNode(JsonNode node)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var property in obj)
                {
                    if (property.Value is not null)
                    {
                        EncryptNode(property.Value);
                    }
                }
                break;

            case JsonArray array:
                foreach (var item in array)
                {
                    if (item is not null)
                    {
                        EncryptNode(item);
                    }
                }
                break;

            case JsonValue value:
                EncryptValue(value);
                break;
        }
    }

    private void EncryptValue(JsonValue value)
    {
        if (value.TryGetValue<string>(out var s) && _options.EncryptStrings)
        {
            ReplaceEncrypted(value, s);
            return;
        }

        if (value.TryGetValue<int>(out var i) && _options.EncryptIntegers)
        {
            ReplaceEncrypted(value, i.ToString());
            return;
        }

        if (value.TryGetValue<bool>(out var b) && _options.EncryptBooleans)
        {
            ReplaceEncrypted(value, b.ToString());
            return;
        }

        if (value.TryGetValue<decimal>(out var d) && _options.EncryptDecimals)
        {
            ReplaceEncrypted(value, d.ToString(System.Globalization.CultureInfo.InvariantCulture));
            return;
        }
    }

    private void ReplaceEncrypted(JsonValue value, string plainText)
    {
        string prefix = _contract.BuildPrefix();

        if (plainText.StartsWith(_contract.Signature, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }
        
        var encrypted = $"{prefix}:{_contract.Provider.Encrypt(plainText)}";

        value.ReplaceWith(JsonValue.Create(encrypted));
    }
}
