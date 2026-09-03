/*
 * Copyright (c) 2026 AndrewM5
 * Licensed under the MIT License.
 * See LICENSE file in the project root for full license information.
 */

using TestApp.Demos;
using TestApp.HostSetup;

namespace TestApp;

public class Program
{
    public static async Task Main(string[] args)
    {
        var decryptedConfig = new ConfigurationSetup().BuildDecryptedConfiguration();

        // Defaults to the standard Microsoft.Extensions.Logging pipeline (both host setups below
        // wire up Console logging on their own without this). Uncomment these two lines to opt
        // into Serilog as an extra logging setup instead.
        SerilogLoggerSetup? serilogSetup = null;
        //serilogSetup = new SerilogLoggerSetup();
        //serilogSetup.ConfigureGlobalLogger();

        try
        {
            // 1. Pick one host setup by uncommenting it — both register the same DevKit modules
            // and Initialize() the same Service_Xxx facades the demos below read from; they differ
            // only in the mechanism used to build/host the underlying IServiceProvider. Toggle the
            // matching StopAsync() call below together with whichever one you start.
            var regularHost = new RegularHostSetup();
            await regularHost.StartAsync(args, decryptedConfig, serilogSetup);
            //var onDemandHost = new OnDemandHostSetup();
            //await onDemandHost.StartAsync(decryptedConfig, serilogSetup);

            // 2. Pick one demo to run by uncommenting it — each one reaches into its own
            // Service_Xxx static facade for whatever it needs (same modules Initialize()'d above).
            //await new CoreClassesDemo().RunAsync();
            //await new ProcessLauncherDemo().RunAsync();
            //await new ApiManagementDemo().RunAsync();
            //await new CredentialManagementDemo().RunAsync();
            //await new CredentialManagementCompositionDemo().RunAsync();
            //await new OAuth2AuthenticationDemo().RunAsync();
            //await new ThreadSafeItemsDemo().RunAsync();
            //await new TaskManagementDemo().RunAsync();
            //await new TaskManagementSyncDemo().RunAsync();
            //await new TaskManagementAsyncDemo().RunAsync();
            //await new AdhocDemo().RunAsync();

            Console.WriteLine("Press enter to exit");
            Console.ReadLine();

            // 3. Gracefully stop the background workers once your main work finishes — must match
            // whichever host setup was started above.
            await regularHost.StopAsync();
            //await onDemandHost.StopAsync();
        }
        finally
        {
            serilogSetup?.Shutdown();
        }
    }
}
