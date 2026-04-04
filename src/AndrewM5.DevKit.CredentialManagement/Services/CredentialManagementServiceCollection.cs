using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;

namespace AndrewM5.DevKit.CredentialManagement.Services;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> to register credential management services.
/// </summary>
public static class CredentialManagementServiceCollection
{
    /// <summary>
    /// Configures Data Protection and registers <see cref="FileSecretStore"/> as a singleton service.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="applicationName">The unique name of the application, used for both Data Protection and the store identity.</param>
    /// <param name="secretsFolder">The directory path where the encrypted secret files will be stored.</param>
    /// <param name="keysFolder">The directory path where the Data Protection XML master keys will be persisted.</param>
    /// <returns>The original <see cref="IServiceCollection"/> for chaining further configuration calls.</returns>
    /// <remarks>
    /// This method sets up the <see cref="IDataProtectionProvider"/> to persist keys to the file system at 
    /// <paramref name="keysFolder"/>. Ensure this folder has restricted access permissions.
    /// </remarks>
    public static IServiceCollection AddFileSecretStore(this IServiceCollection services, string applicationName, string secretsFolder, string keysFolder)
    {
        services.AddDataProtection()
                .SetApplicationName(applicationName)
                .PersistKeysToFileSystem(new DirectoryInfo(keysFolder));

        services.AddSingleton(sp =>
            new FileSecretStore(sp.GetRequiredService<IDataProtectionProvider>(), applicationName, secretsFolder)
        );

        return services;
    }   
}
