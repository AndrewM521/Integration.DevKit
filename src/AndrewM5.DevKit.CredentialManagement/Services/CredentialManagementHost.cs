using Microsoft.Extensions.DependencyInjection;

namespace AndrewM5.DevKit.CredentialManagement.Services;

/// <summary>
/// Provides a static entry point for the Credential Management module.
/// </summary>
/// <remarks>
/// This host acts as a static wrapper for services resolved from the Dependency Injection container. 
/// It must be initialized during application startup (e.g., in Program.cs or Startup.cs) 
/// after the service provider has been built.
/// </remarks>
public static class CredentialManagementHost
{
    private const string NoInitSuffix = " has not been initialized.";

    private static FileSecretStore? _fileSecretStore;

    /// <summary>
    /// Initializes the static <see cref="FileSecretStore"/> instance by resolving it from the provided service provider.
    /// </summary>
    /// <param name="sp">The <see cref="IServiceProvider"/> containing the registered <see cref="FileSecretStore"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="sp"/> is null.</exception>
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
    /// <returns>The singleton-like instance of the <see cref="FileSecretStore"/> resolved during initialization.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown if <see cref="InitializeFileSecretStore"/> was not called before accessing this property.
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
