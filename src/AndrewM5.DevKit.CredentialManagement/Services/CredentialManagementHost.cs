using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.CredentialManagement.Services;

/// <summary>
/// Provides a static entry point for accessing credential management services.
/// This host must be initialized during application startup to provide access to the registered store implementations.
/// </summary>
public static class CredentialManagementHost
{
    private const string NoInitSuffix = " has not been initialized.";

    private static FileSecretStore? _fileSecretStore;

    /// <summary>
    /// Initializes the static file secret store instance by resolving it from the provided service provider.
    /// </summary>
    /// <param name="sp">The <see cref="IServiceProvider"/> used to resolve the <see cref="FileSecretStore"/>.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <see cref="FileSecretStore"/> is not registered in the service collection.
    /// </exception>
    public static void InitializeFileSecretStore(IServiceProvider sp)
    {
        _fileSecretStore = sp.GetService<FileSecretStore>();
        if (_fileSecretStore == null)
        {
            throw new InvalidOperationException($"{typeof(FileSecretStore).Name} is not registered, make sure to call AddFileSecretStore() when configuring services.");
        }
    }

    /// <summary>
    /// Gets the globally accessible instance of the <see cref="FileSecretStore"/>.
    /// </summary>
    /// <value>The current <see cref="FileSecretStore"/> instance.</value>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <see cref="InitializeFileSecretStore"/> has not been called prior to access.
    /// </exception>
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
