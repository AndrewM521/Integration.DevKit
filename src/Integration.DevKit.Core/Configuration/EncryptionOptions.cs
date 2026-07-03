namespace Integration.DevKit.Core;

public sealed class EncryptionOptions
{
    public string FileName { get; set; } = "appsettings.json";
    public string EncryptSigniture { get; set; } = "ENC";
    public bool ThrowOnMissingFile { get; set; } = true;
    public bool EncryptStrings { get; set; } = true;
    public bool EncryptIntegers { get; set; } = false;
    public bool EncryptBooleans { get; set; } = false;
    public bool EncryptDecimals { get; set; } = false;

    public Dictionary<string, IConfigProtector> TargetPaths = new Dictionary<string, IConfigProtector>();

    public void Encrypt(string jsonPath, IConfigProtector? protector = null)
    {
        if (protector == null)
        {
            protector = new Base64ConfigProtector();
        }

        TargetPaths[jsonPath] = protector;
    }
}
