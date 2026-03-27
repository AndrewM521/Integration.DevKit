using AndrewM5.DevKit.Core.Results;

namespace AndrewM5.DevKit.CredentialManagement.Contracts.Interfaces;

public interface ISecretStore
{
    public string StoreName { get; }

    public NullOperationResult SetKey(string fileName, string key, string value);

    public OperationResult<string> GetKey(string fileName, string key);

    public NullOperationResult DeleteKey(string fileName, string key);

    public NullOperationResult DeleteSecret(string fileName);
}
