using Autofac;
using Autofac.Configuration;
using Blockchain;
using CorrelationId.DependencyInjection;
using HealthChecks.UI.Client;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using NetTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NSwag;
using NSwag.AspNetCore;
using NSwag.Generation.Processors.Security;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Raven.Client.Documents;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace AuditServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration) => Configuration = configuration;

        public IConfiguration Configuration { get; }

        public ILifetimeScope AutofacContainer { get; private set; }

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

            NLog.GlobalDiagnosticsContext.Set("AzureLogStorageConnectionString", Configuration["LogStorage:AzureStorage"]);
            NLog.GlobalDiagnosticsContext.Set("LokiConnectionString", Configuration["LogStorage:Loki:Url"]);
            NLog.GlobalDiagnosticsContext.Set("LokiUsername", Configuration["LogStorage:Loki:Username"]);
            NLog.GlobalDiagnosticsContext.Set("LokiPassword", Configuration["LogStorage:Loki:Password"]);
            NLog.GlobalDiagnosticsContext.Set("AppName", Configuration["DataProtection:AppName"]);

            services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                .AddIdentityServerAuthentication(x =>
                {
                    x.Authority = Configuration["Authentication:Authority"];
                    x.ApiSecret = Configuration["Authentication:ApiSecret"];
                    x.ApiName = Configuration["Authentication:ApiName"];
                    x.SupportedTokens = SupportedTokens.Both;
                    x.RequireHttpsMetadata = true;
                    x.EnableCaching = true;
                    x.CacheDuration = TimeSpan.FromMinutes(1);
                    x.NameClaimType = "sub";
                });

            services.AddCorrelationId();

            services.AddSingleton<ITransactionPool, TransactionPool>();
            services.AddSingleton((ctx) => new DocumentStore
            {
                Urls = new[] { Configuration["Raven:Url"] },
                Database = Configuration["Raven:Database"],
                Certificate = Configuration.GetSection("Raven:EncryptionEnabled").Get<bool>() ? new X509Certificate2(Configuration["Raven:CertFile"], Configuration["Raven:CertPassword"]) : null
            }.Initialize());

            services.AddHealthChecks()
                .AddRavenDB(setup => { setup.Urls = new[] { Configuration["Raven:Url"] }; setup.Database = Configuration["Raven:Database"]; setup.Certificate = Configuration.GetSection("Raven:EncryptionEnabled").Get<bool>() ? new X509Certificate2(Configuration["Raven:CertFile"], Configuration["Raven:CertPassword"]) : null; }, "ravendb");

            services.AddOpenApiDocument(config =>
            {
                config.DocumentName = "v1";
                config.PostProcess = document =>
                {
                    document.Info.Version = "v1";
                    document.Info.Title = "BeyondAuth Audit Server";
                    document.Info.Description = File.ReadAllText("readme.md");
                    document.Info.ExtensionData = new Dictionary<string, object>
                    {
                        { "x-logo", new { url = "/logo.png", altText = "BeyondAuth" } }
                    };
                    //document.ExtensionData.Add("x-tagGroups", new { name = "General", tags = new [] { "Discovery", "Tickets" }});

                    document.Info.Contact = new OpenApiContact
                    {
                        Name = "opsAI Support",
                        Email = "support@ops.ai",
                        Url = "https://support.ops.ai"
                    };
                };

                config.AddSecurity("bearer", Enumerable.Empty<string>(), new OpenApiSecurityScheme
                {
                    Type = OpenApiSecuritySchemeType.OAuth2,
                    Description = "Auth",
                    Flow = OpenApiOAuth2Flow.AccessCode,
                    OpenIdConnectUrl = $"{Configuration["Authentication:Authority"]}/.well-known/openid-configuration",
                    Flows = new OpenApiOAuthFlows
                    {
                        AuthorizationCode = new OpenApiOAuthFlow
                        {
                            AuthorizationUrl = $"{Configuration["Authentication:Authority"]}/connect/authorize",
                            TokenUrl = $"{Configuration["Authentication:Authority"]}/connect/token"
                        }
                    }
                });
                config.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("bearer"));

                config.SerializerSettings = new JsonSerializerSettings
                {
                    ContractResolver = new CamelCasePropertyNamesContractResolver(),
                };
                config.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
            });

            services.AddControllers();

            services.AddOpenTelemetryTracing(
                (builder) => builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("beyondauth-audit"))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    //.AddOtlpExporter(opt => opt.Endpoint = new Uri("grafana-agent:55680"))
                    .AddConsoleExporter()
                    );
        }

        /// <summary>
        /// ConfigureContainer is where you can register things directly
        /// with Autofac. This runs after ConfigureServices so the things
        /// here will override registrations made in ConfigureServices.
        /// </summary>
        /// <param name="builder"></param>
        public void ConfigureContainer(ContainerBuilder builder)
        {
            var module = new ConfigurationModule(Configuration);
            builder.RegisterModule(module);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseForwardedHeaders();

            app.UseCors(x => x.AllowAnyOrigin().WithHeaders("accept", "authorization", "content-type", "origin").AllowAnyMethod());

            app.UseXContentTypeOptions();
            app.UseXDownloadOptions();
            app.UseXfo(options => options.SameOrigin());
            app.UseXXssProtection(options => options.EnabledWithBlockMode());
            app.UseHsts(options => options.MaxAge(30).AllResponses());

            app.UseRouting();

            app.UseAuthorization();

            app.UseOpenApi();
            app.UseSwaggerUi3(options =>
            {
                options.OAuth2Client = new OAuth2ClientSettings
                {
                    ClientId = "swagger",
                    AppName = "auditserver",
                    UsePkceWithAuthorizationCodeGrant = true,
                };
            });

            app.UseHealthChecks(Configuration["HealthChecks:FullEndpoint"], new HealthCheckOptions()
            {
                Predicate = _ => true,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            app.UseHealthChecks(Configuration["HealthChecks:SummaryEndpoint"], new HealthCheckOptions()
            {
                Predicate = _ => _.FailureStatus == HealthStatus.Unhealthy
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
