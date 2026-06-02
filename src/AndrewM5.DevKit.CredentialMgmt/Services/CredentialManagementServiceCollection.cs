/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.DataProtection;

namespace AndrewM5.DevKit.CredentialMgmt.Services;

/// <summary>
/// Provides extension methods for <see cref="IServiceCollection"/> to register credential management services.
/// </summary>
public static class CredentialManagementServiceCollection
{
    /// <summary>
    /// Configures ASP.NET Core Data Protection and registers <see cref="FileSecretStore"/> as a singleton service.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="applicationName">The unique name of the application. This is used as the purpose string for Data Protection and the identity of the store.</param>
    /// <param name="secretsFolder">The directory path where the encrypted secret files will be stored.</param>
    /// <param name="keysFolder">The directory path where the Data Protection XML master keys will be persisted.</param>
    /// <returns>The modified <see cref="IServiceCollection"/> for fluent chaining.</returns>
    /// <remarks>
    /// <para>
    /// This method configures the <see cref="IDataProtectionProvider"/> to persist cryptographic keys to the file system at 
    /// the location specified by <paramref name="keysFolder"/>. 
    /// </para>
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
