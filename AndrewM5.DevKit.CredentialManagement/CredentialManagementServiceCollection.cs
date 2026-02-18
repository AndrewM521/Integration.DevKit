using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;
using AndrewM5.DevKit.CredentialManagement.Services;

namespace AndrewM5.DevKit.CredentialManagement;

public static class CredentialManagementServiceCollection
{
    public static IServiceCollection AddFileSecretStore(this IServiceCollection services, string applicationName, string secretsFolder, string keysFolder)
    {
        services.AddDataProtection()
                .SetApplicationName(applicationName)
                .PersistKeysToFileSystem(new DirectoryInfo(keysFolder));

        services.AddSingleton<FileSecretStore>(sp =>
            new FileSecretStore(sp.GetRequiredService<IDataProtectionProvider>(), applicationName, secretsFolder)
        );

        return services;
    }   
}
