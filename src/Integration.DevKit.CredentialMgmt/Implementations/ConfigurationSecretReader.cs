/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.Core;
using Integration.DevKit.CredentialMgmt.Contracts;
using Microsoft.Extensions.Configuration;

namespace Integration.DevKit.CredentialMgmt.Implementations;

/// <summary>
/// An <see cref="ISecretReader"/> that reads values from an <see cref="IConfiguration"/> tree.
/// </summary>
/// <remarks>
/// This makes any <see cref="IConfiguration"/> provider a valid secret source without writing
/// provider-specific code: ASP.NET Core User Secrets in development, environment variables
/// (via the double-underscore-to-colon normalization <see cref="IConfiguration"/> already performs),
/// command-line arguments, or any custom provider a consuming project has registered.
/// </remarks>
public class ConfigurationSecretReader : ISecretReader
{
    private readonly IConfiguration _configuration;

    /// <inheritdoc />
    public string StoreName { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ConfigurationSecretReader"/> class.
    /// </summary>
    /// <param name="configuration">The configuration tree to read secrets from.</param>
    /// <param name="storeName">
    /// The display name for this reader. Defaults to "ConfigurationSecretReader".
    /// </param>
    public ConfigurationSecretReader(IConfiguration configuration, string storeName = "ConfigurationSecretReader")
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        StoreName = storeName;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Looks up <c>{fileName}:{key}</c> as a hierarchical configuration path. This matches both
    /// nested JSON configuration (<c>"fileName": { "key": "value" }</c>) and environment variables
    /// named <c>fileName__key</c>, since <see cref="IConfiguration"/> normalizes double underscores
    /// to colons for the environment variable provider.
    /// </remarks>
    public OperationResult<string> GetKey(string fileName, string key)
    {
        var result = new OperationResult<string>();

        try
        {
            var value = _configuration[$"{fileName}:{key}"];

            if (string.IsNullOrEmpty(value))
            {
                throw new KeyNotFoundException($"Secret '{key}' not found in container '{fileName}' via {StoreName}");
            }

            return result.SetMethodSuccess(value);
        }
        catch (Exception ex)
        {
            return result.SetMethodFailure(ex);
        }
    }
}
