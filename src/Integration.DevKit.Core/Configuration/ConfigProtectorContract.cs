
namespace Integration.DevKit.Core;

public sealed class ConfigProtectorContract
{
    private readonly string _signature = "ENC";
    private readonly string _version = "v1";

    public ConfigProtectorContract(char delimiter = ':')
    {
        Delimiter = delimiter;
    }

    public char Delimiter { get; }

    public string Signature
    {
        get { return _signature; }
        init {
            if (value == null) throw new ArgumentNullException(nameof(value));

            // Throw if the delimiter is found anywhere in the middle of the signature
            if (value.Contains(Delimiter))
            {
                throw new ArgumentException($"Signature '{value}' cannot contain the configured delimiter '{Delimiter}'.");
            }

            _signature = value;
        }
    }

    public string Version {
        get { return _version; }
        init {
            if (value == null) throw new ArgumentNullException(nameof(value));

            // Throw if the delimiter is found anywhere in the middle of the version
            if (value.Contains(Delimiter))
            {
                throw new ArgumentException($"Version '{value}' cannot contain the configured delimiter '{Delimiter}'.");
            }

            _version = value;
        }
    }

    public string BuildPrefix(IConfigProtector protector)
    {
        return $"{Signature}{Version}{Delimiter}{protector.Name}{Delimiter}";
    }
}
