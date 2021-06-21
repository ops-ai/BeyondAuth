using Autofac.Extensions.DependencyInjection;
using Azure.Identity;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Web;
using System;
using OpenTelemetry.Logs;

namespace BeyondAuth.Web
{
    public class Program
    {
        public static void Main(string[] args) => CreateHostBuilder(args).Build().Run();

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((context, config) =>
                {
                    if (Environment.GetEnvironmentVariable("VaultUri") != null)
                        config.AddAzureKeyVault(new Uri(Environment.GetEnvironmentVariable("VaultUri")), new DefaultAzureCredential());
                })
                .UseServiceProviderFactory(new AutofacServiceProviderFactory())
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
