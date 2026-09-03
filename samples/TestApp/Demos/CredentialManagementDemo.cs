/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.CredentialMgmt;

namespace TestApp.Demos;

/// <summary>
/// Set/get/delete-key/delete-secret round trip against the configured <see cref="FileSecretStore"/>.
/// </summary>
public class CredentialManagementDemo : IDemo
{
    public Task RunAsync()
    {
        Console.WriteLine("----|----|Credential Management|----|----");
        var store = Service_CredentialMgmt.FileSecretStore;

        string fileName = "Api";
        string initialKey = "ApiKey";
        string initialValue = "super-secret-value";
        // Set a key
        var setResult = store.SetKey(fileName, initialKey, initialValue);
        if (setResult.MethodSuccess)
        {
            Console.WriteLine($"Set value: {initialValue}");
        }
        else
        {
            Console.WriteLine($"Set failed: {setResult.Exception}");
            return Task.CompletedTask;
        }

        // Get a key
        var getResult = store.GetKey(fileName, initialKey);
        if (getResult.MethodSuccess)
        {
            Console.WriteLine($"Retrieved value: {getResult.Result}");
        }
        else
        {
            Console.WriteLine($"Get failed: {getResult.Exception}");
        }

        // Delete the key
        var deleteKeyResult = store.DeleteKey(fileName, initialKey);
        if (deleteKeyResult.MethodSuccess)
        {
            Console.WriteLine($"Deleted file {fileName}");
        }
        else
        {
            Console.WriteLine($"Delete failed: {deleteKeyResult.Exception}");
        }

        Console.WriteLine("Key deleted successfully.");

        // Verify deletion
        var verifyResult = store.GetKey(fileName, initialKey);
        if (!verifyResult.MethodSuccess)
        {
            Console.WriteLine("Verified: key no longer exists.");
        }
        else
        {
            Console.WriteLine("DeleteKey failed — key still exists.");
        }

        // Delete secret file
        var deleteResult = store.DeleteSecret(fileName);
        if (deleteResult.MethodSuccess)
        {
            Console.WriteLine($"Deleted file {fileName}");
        }
        else
        {
            Console.WriteLine($"Delete failed: {deleteResult.Exception}");
        }

        return Task.CompletedTask;
    }
}
