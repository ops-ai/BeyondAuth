using Azure.Storage.Blobs;
using Imageflow.Server;
using Imageflow.Server.Storage.AzureBlob;
using JSNLog;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.SpaServices.AngularCli;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetTools;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System.Collections.Generic;

namespace BeyondAuth.Web
{
    public class Startup
    {
        public Startup(IConfiguration configuration) => Configuration = configuration;

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardLimit = 1;
                options.ForwardedForHeaderName = Configuration["Proxy:HeaderName"];
                options.ForwardedHeaders = ForwardedHeaders.All;
                Configuration.GetSection("Proxy:Networks").Get<List<string>>().ForEach(ipNetwork =>
                {
                    if (IPAddressRange.TryParse(ipNetwork, out IPAddressRange range))
                        options.KnownNetworks.Add(new IPNetwork(range.Begin, range.GetPrefixLength()));
                });
            });

            services.AddControllersWithViews();
            // In production, the Angular files will be served from this directory
            services.AddSpaStaticFiles(configuration =>
            {
                configuration.RootPath = "ClientApp/dist";
            });

            services.AddImageflowAzureBlobService(new AzureBlobServiceOptions(Configuration["Azure:PhotoStorageConnectionString"], new BlobClientOptions()).MapPrefix("/photos", "beyondauth"));

            services.AddOpenTelemetryTracing(
                (builder) => builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("beyondauth-saas"))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    //.AddOtlpExporter(opt => opt.Endpoint = new Uri("grafana-agent:55680"))
                    .AddConsoleExporter()
                    );
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            // Configure JSNLog
            var jsnlogConfiguration = new JsnlogConfiguration();
            app.UseJSNLog(new LoggingAdapter(loggerFactory), jsnlogConfiguration);

            app.UseHttpsRedirection();

            app.UseForwardedHeaders();

            app.UseStaticFiles();
            if (!env.IsDevelopment())
            {
                app.UseSpaStaticFiles();
            }

            app.UseImageflow(new ImageflowMiddlewareOptions()
                .SetMapWebRoot(true)
                .SetLicenseKey(EnforceLicenseWith.RedDotWatermark, Configuration["Imazen:License"])
                .SetDiagnosticsPageAccess(env.IsDevelopment() ? AccessDiagnosticsFrom.AnyHost : AccessDiagnosticsFrom.LocalHost)
                .SetDiagnosticsPagePassword(Configuration["Imazen:DiagnosticsPassword"])
                .SetDefaultCacheControlString("public, max-age=2592000")
                .AddCommandDefault("webp.quality", "90")
                .SetUsePresetsExclusively(true)
                .AddPreset(new PresetOptions("large", PresetPriority.DefaultValues)
                    .SetCommand("width", "1024")
                    .SetCommand("height", "1024")
                    .SetCommand("mode", "max"))
                                );

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller}/{action=Index}/{id?}");
            });

            app.UseSpa(spa =>
            {
                // To learn more about options for serving an Angular SPA from ASP.NET Core,
                // see https://go.microsoft.com/fwlink/?linkid=864501

                spa.Options.SourcePath = "ClientApp";

                if (env.IsDevelopment())
                {
                    spa.UseAngularCliServer(npmScript: "start");
                }
            });
        }
    }
}
