using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Integration.DevKit.Core;

public sealed class JsonEncryptor
{
    private readonly ConfigProtectorContract _contract;
    private readonly EncryptionOptions _options;

    public JsonEncryptor(ConfigProtectorContract contract, EncryptionOptions options)
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

        IConfigProtector defaultProvider = new Base64ConfigProtector();
        foreach (var path in _options.TargetPaths)
        {
            var activeProvider = defaultProvider;

            if (path.Value != null)
            {
                activeProvider = path.Value;
            }

            if (activeProvider.Name.Contains(_contract.Delimiter))
            {
                throw new InvalidOperationException($"Protector Name '{activeProvider.Name}' cannot contain the configured delimiter '{_contract.Delimiter}'.");
            }

            EncryptNode(root, path.Key, activeProvider);
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

    private void EncryptNode(JsonObject root, string path, IConfigProtector protector)
    {
        var targetPathParts = path.Split(":");
        JsonNode? currentNode = root;

        foreach (var part in targetPathParts)
        {
            currentNode = currentNode?[part];

            if (currentNode is null)
            {
                return;
            }
        }

        switch (currentNode)
        {
            case JsonObject obj:
                EncryptNode(obj, protector);
                break;

            case JsonValue value:
                EncryptValue(value, protector);
                break;

            case JsonArray array:
                EncryptNode(array, protector);
                break;
        }
    }

    private void EncryptNode(JsonNode node, IConfigProtector protector)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var property in obj)
                {
                    if (property.Value is not null)
                    {
                        EncryptNode(property.Value, protector);
                    }
                }
                break;

            case JsonArray array:
                foreach (var item in array)
                {
                    if (item is not null)
                    {
                        EncryptNode(item, protector);
                    }
                }
                break;

            case JsonValue value:
                EncryptValue(value, protector);
                break;
        }
    }

    private void EncryptValue(JsonValue value, IConfigProtector protector)
    {
        if (value.TryGetValue<string>(out var s) && _options.EncryptStrings)
        {
            ReplaceEncrypted(value, s, protector);
            return;
        }

        if (value.TryGetValue<int>(out var i) && _options.EncryptIntegers)
        {
            ReplaceEncrypted(value, i.ToString(), protector);
            return;
        }

        if (value.TryGetValue<bool>(out var b) && _options.EncryptBooleans)
        {
            ReplaceEncrypted(value, b.ToString(), protector);
            return;
        }

        if (value.TryGetValue<decimal>(out var d) && _options.EncryptDecimals)
        {
            ReplaceEncrypted(value, d.ToString(System.Globalization.CultureInfo.InvariantCulture), protector);
            return;
        }
    }

    private void ReplaceEncrypted(JsonValue value, string plainText, IConfigProtector protector)
    {
        if (plainText.StartsWith(_contract.Signature, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        string prefix = _contract.BuildPrefix(protector);

        var encrypted = $"{prefix}{protector.Encrypt(plainText)}";

        value.ReplaceWith(JsonValue.Create(encrypted));
    }
}
