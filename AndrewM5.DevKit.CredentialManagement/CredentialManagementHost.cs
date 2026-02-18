using AndrewM5.DevKit.CredentialManagement.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.CredentialManagement;

public static class CredentialManagementHost
{
    private const string NoInitSuffix = " has not been initialized.";

    private static FileSecretStore? _fileSecretStore;

    public static void InitalizeFileSecretStore(IServiceProvider sp)
    {
        _fileSecretStore = sp.GetService<FileSecretStore>();
        if (_fileSecretStore == null)
        {
            throw new InvalidOperationException($"{typeof(FileSecretStore).Name} is not registered, make sure to call AddFileSecretStore() when configuring services.");
        }
    }

    public static FileSecretStore FileSecretStore
    {
        get
        {
            if (_fileSecretStore == null)
            {
                throw new InvalidOperationException("FileSecretStore" + NoInitSuffix);
            }

            return _fileSecretStore;
        }
    }
}
