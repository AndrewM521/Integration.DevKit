/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.CredentialMgmt;
using Integration.DevKit.CredentialMgmt.Contracts;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace TestApp.Demos;

/// <summary>
/// Demonstrates <see cref="ConfigurationSecretReader"/>, <see cref="CompositeSecretReader"/> fallback/first-match,
/// <see cref="SecretStoreExtensions.ImportFrom"/>, and config-driven <c>AddCredentialMgmt</c> registration.
/// </summary>
public class CredentialManagementCompositionDemo : IDemo
{
    public Task RunAsync()
    {
        Console.WriteLine("----|----|Credential Management - Composition|----|----");

        // Stand-in for env vars / User Secrets: any IConfiguration is a valid secret source.
        var inMemoryConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Api:ApiKey"] = "config-sourced-value"
            })
            .Build();

        var configReader = new ConfigurationSecretReader(inMemoryConfig);

        // 1. ConfigurationSecretReader on its own.
        var configResult = configReader.GetKey("Api", "ApiKey");
        Console.WriteLine(configResult.MethodSuccess
            ? $"ConfigurationSecretReader found: {configResult.Result}"
            : $"ConfigurationSecretReader failed: {configResult.Exception.Message}");

        var missingResult = configReader.GetKey("Api", "DoesNotExist");
        Console.WriteLine(missingResult.MethodSuccess
            ? "Unexpected success reading a missing key"
            : $"Expected failure reading missing key: {missingResult.Exception.GetType().Name}");

        // 2. CompositeSecretReader: tries configReader first, falls back to the FileSecretStore.
        var fileStore = Service_CredentialMgmt.FileSecretStore;
        fileStore.SetKey("Composite", "DbPassword", "file-sourced-value");

        var composite = new CompositeSecretReader(new ISecretReader[] { configReader, fileStore });

        var compositeFallback = composite.GetKey("Composite", "DbPassword");
        Console.WriteLine(compositeFallback.MethodSuccess
            ? $"CompositeSecretReader fell through to FileSecretStore: {compositeFallback.Result}"
            : $"CompositeSecretReader failed: {compositeFallback.Exception.Message}");

        var compositeFirstMatch = composite.GetKey("Api", "ApiKey");
        Console.WriteLine(compositeFirstMatch.MethodSuccess
            ? $"CompositeSecretReader resolved from ConfigurationSecretReader first: {compositeFirstMatch.Result}"
            : $"CompositeSecretReader failed: {compositeFirstMatch.Exception.Message}");

        // 3. ImportFrom: seed the encrypted FileSecretStore from the config reader, once.
        var importResult = fileStore.ImportFrom(configReader, "Api", "ApiKey");
        Console.WriteLine(importResult.MethodSuccess
            ? "ImportFrom succeeded — ApiKey now lives in the encrypted FileSecretStore"
            : $"ImportFrom failed: {importResult.Exception.Message}");

        var verifyImport = fileStore.GetKey("Api", "ApiKey");
        Console.WriteLine(verifyImport.MethodSuccess
            ? $"FileSecretStore now has imported value: {verifyImport.Result}"
            : $"Verify import failed: {verifyImport.Exception.Message}");

        // Cleanup so re-running this demo is idempotent.
        fileStore.DeleteKey("Composite", "DbPassword");
        fileStore.DeleteKey("Api", "ApiKey");

        // 4. AddCredentialMgmt: config-driven registration, in a throwaway service collection.
        var credentialMgmtConfig = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Integration.DevKit:CredentialManagement:Provider"] = "File",
                ["Integration.DevKit:CredentialManagement:Options:ApplicationName"] = "TestApp-ConfigDriven",
                ["Integration.DevKit:CredentialManagement:Options:SecretsFolder"] = "C:\\Users\\andre\\Projects\\Junk\\Secrets",
                ["Integration.DevKit:CredentialManagement:Options:KeysFolder"] = "C:\\Users\\andre\\Projects\\Junk\\Keys"
            })
            .Build();

        var standaloneServices = new ServiceCollection();
        standaloneServices.AddCredentialMgmt(credentialMgmtConfig);
        using var standaloneProvider = standaloneServices.BuildServiceProvider();

        var configDrivenStore = standaloneProvider.GetRequiredService<FileSecretStore>();
        Console.WriteLine($"AddCredentialMgmt registered a FileSecretStore named '{configDrivenStore.StoreName}'");

        return Task.CompletedTask;
    }
}
