/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.CredentialMgmt.Contracts;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Integration.DevKit.CredentialMgmt;

/// <summary>
/// Provides a static entry point for the Credential Management module.
/// </summary>
/// <remarks>
/// This acts as a static wrapper for services resolved from the Dependency Injection container. 
/// It must be registered and initialized during application startup (e.g., in Program.cs or Startup.cs)
/// </remarks>
public static class Service_CredentialMgmt
{
    private const string NoInitSuffix = " has not been initialized.";

    private static FileSecretStore? _fileSecretStore;

    /// <summary>
    /// Configures ASP.NET Core Data Protection and registers <see cref="FileSecretStore"/> as a singleton service.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="applicationName">The unique name of the application. This is used as the purpose string for Data Protection and the identity of the store.</param>
    /// <param name="secretsFolder">The directory path where the encrypted secret files will be stored.</param>
    /// <param name="keysFolder">The directory path where the Data Protection XML master keys will be persisted.</param>
    /// <returns>The modified <see cref="IServiceCollection"/> for fluent chaining.</returns>
    public static IServiceCollection AddFileSecretStore(this IServiceCollection services, string applicationName, string secretsFolder, string keysFolder)
    {
        services.AddDataProtection()
                .SetApplicationName(applicationName)
                .PersistKeysToFileSystem(new DirectoryInfo(keysFolder));

        services.TryAddSingleton(sp =>
            new FileSecretStore(sp.GetRequiredService<IDataProtectionProvider>(), applicationName, secretsFolder)
        );

        return services;
    }

    /// <summary>
    /// Registers the credential management backend selected by configuration.
    /// </summary>
    /// <remarks>
    /// Reads the <c>Integration.DevKit:CredentialManagement</c> section (see <see cref="CredentialManagementSettings"/>)
    /// and registers the corresponding backend, matching the config-driven registration convention used by the
    /// other DevKit modules (e.g. <c>AddSQLMgmt</c>). Currently only the <c>"File"</c> provider is supported,
    /// which delegates to <see cref="AddFileSecretStore"/> using the bound <see cref="FileSecretStoreSettings"/>.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configuration">The application configuration used to bind <see cref="CredentialManagementSettings"/>.</param>
    /// <returns>The modified <see cref="IServiceCollection"/> for fluent chaining.</returns>
    /// <exception cref="NotSupportedException">Thrown if <see cref="CredentialManagementSettings.Provider"/> names an unsupported backend.</exception>
    public static IServiceCollection AddCredentialMgmt(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = new CredentialManagementSettings();
        configuration.GetSection("Integration.DevKit:CredentialManagement").Bind(settings);

        switch (settings.Provider)
        {
            case "File":
                services.AddFileSecretStore(settings.File.ApplicationName, settings.File.SecretsFolder, settings.File.KeysFolder);
                break;
            default:
                throw new NotSupportedException($"CredentialManagement provider '{settings.Provider}' is not supported.");
        }

        return services;
    }

    /// <summary>
    /// Initializes the static <see cref="FileSecretStore"/> instance.
    /// </summary>
    /// <param name="sp">The <see cref="IServiceProvider"/> containing the registered <see cref="FileSecretStore"/>.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="sp"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if <see cref="FileSecretStore"/> is not registered in the service collection.</exception>
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
