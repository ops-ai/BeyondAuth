using Azure.Core;
using Azure.Identity;
using NLog.Web;
using OpenTelemetry.Logs;

namespace IdentityManager
{
    public class Program
    {
        public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    if (Environment.GetEnvironmentVariable("VaultUri") != null)
                    {
                        var keyVaultEndpoint = new Uri(Environment.GetEnvironmentVariable("VaultUri")!);
                        TokenCredential? clientCredential = Environment.GetEnvironmentVariable("ClientId") != null ? new ClientSecretCredential(Environment.GetEnvironmentVariable("TenantId"), Environment.GetEnvironmentVariable("ClientId"), Environment.GetEnvironmentVariable("ClientSecret")) : null;

                        config.AddAzureKeyVault(new Uri(Environment.GetEnvironmentVariable("VaultUri")!), clientCredential ?? new DefaultAzureCredential());
                    }
                })
                .UseContentRoot(Directory.GetCurrentDirectory())
                //.UseServiceProviderFactory(new AutofacServiceProviderFactory())
                .UseNLog()
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
                .ConfigureLogging((context, builder) =>
                {
                    builder.AddNLog("nlog.config").AddNLogWeb();
                    builder.AddConsole();

                    var useLogging = context.Configuration.GetValue<bool>("UseLogging");
                    if (useLogging)
                    {
                        builder.AddOpenTelemetry(options =>
                        {
                            options.IncludeScopes = true;
                            options.ParseStateValues = true;
                            options.IncludeFormattedMessage = true;
                            options.AddConsoleExporter();
                        });
                    }
                });
    }
}
