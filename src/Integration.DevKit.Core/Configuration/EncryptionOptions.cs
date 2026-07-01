using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Integration.DevKit.Core.Configuration;

public sealed class EncryptionOptions
{
    public string FileName { get; set; } = "appsettings.json";
    public string EncryptSigniture { get; set; } = "ENC";
    public bool ThrowOnMissingFile { get; set; } = true;
    public bool EncryptStrings { get; set; } = true;
    public bool EncryptIntegers { get; set; } = false;
    public bool EncryptBooleans { get; set; } = false;
    public bool EncryptDecimals { get; set; } = false;


    internal List<string> Paths { get; } = new List<string>();

    public EncryptionOptions Encrypt(string jsonPath)
    {
        Paths.Add(jsonPath);
        return this;
    }
}
