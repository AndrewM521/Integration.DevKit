/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.CredentialMgmt.Contracts;
using Integration.DevKit.ThreadLocks;
using Integration.DevKit.ThreadLocks.Contracts;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

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
    /// The registry of credential-management providers selectable via <see cref="AddCredentialMgmt"/>,
    /// keyed by the <see cref="CredentialManagementSettings.Provider"/> name that selects them.
    /// </summary>
    /// <remarks>
    /// Seeded with the built-in <c>"File"</c> provider. Consuming applications add their own entries
    /// via <see cref="RegisterProvider"/> — this is what lets a custom <see cref="ISecretStore"/>
    /// (e.g. one backed by a cloud secrets vault) become selectable by name without modifying this library.
    /// <para/>
    /// Each registered delegate is invoked with, in order: the target <see cref="IServiceCollection"/>,
    /// the provider's bound <see cref="CredentialManagementSettings.Options"/> dictionary, the resolved
    /// <see cref="CredentialManagementSettings.EnableLogging"/> value, and the application's
    /// <see cref="IConfiguration"/> — see <see cref="RegisterProvider"/> for the full parameter breakdown.
    /// </remarks>
    private static readonly ConcurrentDictionary<string, Action<IServiceCollection, Dictionary<string, object>, bool, IConfiguration>> _providers = new()
    {
        ["File"] = (services, options, enableLogging, configuration) =>
        {
            var applicationName = options.GetRequiredOption<string>("ApplicationName", "File");
            var secretsFolder = options.GetRequiredOption<string>("SecretsFolder", "File");
            var keysFolder = options.GetRequiredOption<string>("KeysFolder", "File");

            // FileSecretStore needs an IThreadLockManager to serialize concurrent access to the same
            // container's file. TryAddSingleton means this is a no-op if the app already called
            // AddThreadLocks() itself.
            services.AddThreadLocks(configuration);

            services.AddDataProtection()
                    .SetApplicationName(applicationName)
                    .PersistKeysToFileSystem(new DirectoryInfo(keysFolder));

            services.TryAddSingleton(sp =>
                new FileSecretStore(
                    sp.GetRequiredService<IDataProtectionProvider>(),
                    applicationName,
                    secretsFolder,
                    sp.GetRequiredService<IThreadLockManager>(),
                    sp.GetService<ILoggerFactory>(),
                    enableLogging)
            );
        }
    };

    /// <summary>
    /// Registers a credential-management provider so it becomes selectable by name via
    /// <see cref="AddCredentialMgmt"/> (through the <c>"Integration.DevKit:CredentialManagement:Provider"</c>
    /// configuration value), without requiring any change to this library.
    /// </summary>
    /// <remarks>
    /// Call this during application startup, before <see cref="AddCredentialMgmt"/>. Registering under a
    /// name that already has a provider (including the built-in <c>"File"</c> provider) replaces it.
    /// </remarks>
    /// <param name="providerName">
    /// The provider name that will match <see cref="CredentialManagementSettings.Provider"/>. Matched
    /// case-sensitively, consistent with how the built-in <c>"File"</c> provider is matched.
    /// </param>
    /// <param name="register">
    /// Invoked with, in order: the target <see cref="IServiceCollection"/>; the provider's bound
    /// <see cref="CredentialManagementSettings.Options"/> dictionary; the resolved
    /// <see cref="CredentialManagementSettings.EnableLogging"/> value, so a provider can honor the same
    /// logging toggle as the built-in <c>"File"</c> provider does; and the application's
    /// <see cref="IConfiguration"/>, in case the provider needs to read configuration sections of its own
    /// (e.g. to register another DevKit module as a dependency).
    /// Should register whatever the provider's <see cref="ISecretStore"/> or <see cref="ISecretReader"/>
    /// implementation needs (its own dependencies, and itself) — see <see cref="CredentialManagementOptionsExtensions.GetRequiredOption{T}"/>
    /// for reading typed values out of the options dictionary with a clear error on a missing/mistyped option.
    /// </param>
    /// <exception cref="ArgumentException">Thrown if <paramref name="providerName"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="register"/> is null.</exception>
    public static void RegisterProvider(string providerName, Action<IServiceCollection, Dictionary<string, object>, bool, IConfiguration> register)
    {
        if (string.IsNullOrWhiteSpace(providerName))
        {
            throw new ArgumentException("Provider name cannot be null or whitespace.", nameof(providerName));
        }

        if (register == null)
        {
            throw new ArgumentNullException(nameof(register));
        }

        _providers[providerName] = register;
    }

    /// <summary>
    /// Registers the credential management backend selected by configuration.
    /// </summary>
    /// <remarks>
    /// Reads the <c>Integration.DevKit:CredentialManagement</c> section (see <see cref="CredentialManagementSettings"/>)
    /// and invokes whichever provider was registered under <see cref="CredentialManagementSettings.Provider"/>'s name
    /// — either the built-in <c>"File"</c> provider, or a custom provider a consuming application added via
    /// <see cref="RegisterProvider"/> — passing it the bound <see cref="CredentialManagementSettings.Options"/> dictionary.
    /// </remarks>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configuration">The application configuration used to bind <see cref="CredentialManagementSettings"/>.</param>
    /// <returns>The modified <see cref="IServiceCollection"/> for fluent chaining.</returns>
    /// <exception cref="NotSupportedException">
    /// Thrown if <see cref="CredentialManagementSettings.Provider"/> names a provider that has not been registered
    /// (via the built-in <c>"File"</c> provider or a prior call to <see cref="RegisterProvider"/>).
    /// </exception>
    public static IServiceCollection AddCredentialMgmt(this IServiceCollection services, IConfiguration configuration)
    {
        var settings = new CredentialManagementSettings();
        configuration.GetSection("Integration.DevKit:CredentialManagement").Bind(settings);

        if (!_providers.TryGetValue(settings.Provider, out var register))
        {
            throw new NotSupportedException(
                $"CredentialManagement provider '{settings.Provider}' is not registered. " +
                $"Registered providers: {string.Join(", ", _providers.Keys)}. " +
                $"Call {nameof(Service_CredentialMgmt)}.{nameof(RegisterProvider)}(...) before {nameof(AddCredentialMgmt)}() to add a custom provider.");
        }

        register(services, settings.Options, settings.EnableLogging, configuration);

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
            throw new InvalidOperationException(
                $"{typeof(FileSecretStore).Name} is not registered, make sure to call AddCredentialMgmt() with a \"File\" provider (or register one manually) when configuring services.");
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
