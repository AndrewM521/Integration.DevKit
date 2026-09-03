/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using Integration.DevKit.CredentialMgmt;
using Integration.DevKit.RESTApiMgmt;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Reflection;
using System.Text;

namespace TestApp.Demos;

/// <summary>
/// End-to-end <see cref="OAuth2ClientCredentialsAuthStrategy"/> smoke test: token fetch, cache reuse,
/// and forced-expiry refresh, against an in-process fake token endpoint.
/// </summary>
public class OAuth2AuthenticationDemo : IDemo
{
    public async Task RunAsync()
    {
        Console.WriteLine("----|----|REST API - OAuth2 Client Credentials|----|----");

        int tokenEndpointHits = 0;
        var accessTokenCounter = 0;

        // A minimal in-process fake token endpoint — no real IdP needed for this smoke test.
        using var listener = new HttpListener();
        listener.Prefixes.Add("http://127.0.0.1:8734/");
        listener.Start();

        var listenerTask = Task.Run(async () =>
        {
            while (listener.IsListening)
            {
                HttpListenerContext ctx;
                try
                {
                    ctx = await listener.GetContextAsync();
                }
                catch (HttpListenerException)
                {
                    break; // listener was stopped
                }
                catch (ObjectDisposedException)
                {
                    break;
                }

                tokenEndpointHits++;
                accessTokenCounter++;

                var body = $"{{\"access_token\":\"fake-token-{accessTokenCounter}\",\"token_type\":\"Bearer\",\"expires_in\":3600}}";
                var bytes = Encoding.UTF8.GetBytes(body);

                ctx.Response.ContentType = "application/json";
                ctx.Response.ContentLength64 = bytes.Length;
                await ctx.Response.OutputStream.WriteAsync(bytes);
                ctx.Response.Close();
            }
        });

        try
        {
            // Client secret sourced through the Part 1 credential management pieces.
            var oauthConfig = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["OAuth:client_secret"] = "super-secret-value"
                })
                .Build();

            var secretReader = new ConfigurationSecretReader(oauthConfig);

            using var tokenHttpClient = new HttpClient();

            var authStrategy = new OAuth2ClientCredentialsAuthStrategy(
                tokenHttpClient: tokenHttpClient,
                tokenEndpoint: "http://127.0.0.1:8734/",
                clientId: "test-client",
                credentialContainer: "OAuth",
                secretReader: secretReader);

            // 1. First call — should hit the fake token endpoint and set the Authorization header.
            using var request1 = new HttpRequestMessage(HttpMethod.Get, "http://example.invalid/");
            var apply1 = await authStrategy.ApplyAsync(request1);
            Console.WriteLine(apply1.MethodSuccess
                ? $"First ApplyAsync succeeded. Authorization: {request1.Headers.Authorization}"
                : $"First ApplyAsync failed: {apply1.Exception.Message}");
            Console.WriteLine($"Token endpoint hits so far: {tokenEndpointHits} (expected 1)");

            // 2. Second call — token still valid, should NOT hit the fake endpoint again.
            using var request2 = new HttpRequestMessage(HttpMethod.Get, "http://example.invalid/");
            var apply2 = await authStrategy.ApplyAsync(request2);
            Console.WriteLine(apply2.MethodSuccess
                ? $"Second ApplyAsync reused cached token. Authorization: {request2.Headers.Authorization}"
                : $"Second ApplyAsync failed: {apply2.Exception.Message}");
            Console.WriteLine($"Token endpoint hits so far: {tokenEndpointHits} (expected 1, still)");

            // 3. Force the cached token to look expired, then confirm a refresh happens.
            var expiresAtField = typeof(OAuth2ClientCredentialsAuthStrategy)
                .GetField("_expiresAtUtc", BindingFlags.NonPublic | BindingFlags.Instance)!;
            expiresAtField.SetValue(authStrategy, DateTimeOffset.UtcNow.AddMinutes(-10));

            using var request3 = new HttpRequestMessage(HttpMethod.Get, "http://example.invalid/");
            var apply3 = await authStrategy.ApplyAsync(request3);
            Console.WriteLine(apply3.MethodSuccess
                ? $"Third ApplyAsync refreshed the token. Authorization: {request3.Headers.Authorization}"
                : $"Third ApplyAsync failed: {apply3.Exception.Message}");
            Console.WriteLine($"Token endpoint hits so far: {tokenEndpointHits} (expected 2)");
        }
        finally
        {
            listener.Stop();
            listener.Close();
            await listenerTask;
        }
    }
}
