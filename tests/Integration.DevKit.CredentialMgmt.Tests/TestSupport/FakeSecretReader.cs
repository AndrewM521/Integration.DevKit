using Integration.DevKit.Core;
using Integration.DevKit.CredentialMgmt.Contracts;

namespace Integration.DevKit.CredentialMgmt.Tests.TestSupport;

/// <summary>
/// A simple in-memory <see cref="ISecretReader"/> test double keyed by "fileName:key".
/// </summary>
internal sealed class FakeSecretReader : ISecretReader
{
    private readonly Dictionary<string, string> _values = new();

    public string StoreName { get; init; } = "FakeSecretReader";

    public FakeSecretReader With(string fileName, string key, string value)
    {
        _values[$"{fileName}:{key}"] = value;
        return this;
    }

    public OperationResult<string> GetKey(string fileName, string key)
    {
        var result = new OperationResult<string>();

        if (_values.TryGetValue($"{fileName}:{key}", out var value))
        {
            return result.SetMethodSuccess(value);
        }

        return result.SetMethodFailure(new KeyNotFoundException($"Secret '{key}' not found in container '{fileName}'"));
    }
}
