using Autofac;
using Autofac.Configuration;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using BeyondAuth.PolicyProvider;
using CorrelationId.DependencyInjection;
using HealthChecks.UI.Client;
using IdentityServer4.AccessTokenValidation;
using IdentityServer4.Stores.Serialization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NetTools;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NSwag;
using NSwag.AspNetCore;
using NSwag.Generation.Processors.Security;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Prometheus;
using Prometheus.SystemMetrics;
using Prometheus.SystemMetrics.Collectors;
using Raven.Client.Documents;
using Raven.Client.Json.Serialization.NewtonsoftJson;
using System.Security.Cryptography.X509Certificates;

namespace AuthorizationServer
{
    public class Startup
    {
        public Startup(IConfiguration configuration) => Configuration = configuration;

        public IConfiguration Configuration { get; }

        public ILifetimeScope AutofacContainer { get; private set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();

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

            services.AddDefaultCorrelationId(options =>
            {
                options.UpdateTraceIdentifier = true;
            });
            services.AddMemoryCache();

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

            X509Certificate2? ravenDBcert = null;
            if (Environment.GetEnvironmentVariable("VaultUri") != null)
            {
                TokenCredential? clientCredential = Environment.GetEnvironmentVariable("ClientId") != null ? new ClientSecretCredential(Environment.GetEnvironmentVariable("TenantId"), Environment.GetEnvironmentVariable("ClientId"), Environment.GetEnvironmentVariable("ClientSecret")) : null;

                var certificateClient = new CertificateClient(vaultUri: new Uri(Environment.GetEnvironmentVariable("VaultUri")!), credential: clientCredential ?? new DefaultAzureCredential());
                var secretClient = new SecretClient(new Uri(Environment.GetEnvironmentVariable("VaultUri")!), clientCredential ?? new DefaultAzureCredential());

                var ravenDbCertificateClient = certificateClient.GetCertificate("RavenDB");
                var ravenDbCertificateSegments = ravenDbCertificateClient.Value.SecretId.Segments;
                var ravenDbCertificateBytes = Convert.FromBase64String(secretClient.GetSecret(ravenDbCertificateSegments[2].Trim('/'), ravenDbCertificateSegments[3].TrimEnd('/')).Value.Value);
                ravenDBcert = new X509Certificate2(ravenDbCertificateBytes);
            }

            services.AddSingleton((ctx) =>
            {
                IDocumentStore store = new DocumentStore
                {
                    Urls = Configuration.GetSection("Raven:Urls").Get<string[]>(),
                    Database = Configuration["Raven:Database"],
                    Certificate = ravenDBcert
                };

                var serializerConventions = new NewtonsoftJsonSerializationConventions();
                serializerConventions.CustomizeJsonSerializer += (JsonSerializer serializer) =>
                {
                    serializer.Converters.Add(new ClaimConverter());
                    serializer.Converters.Add(new ClaimsPrincipalConverter());
                };

                store.Conventions.Serialization = serializerConventions;

                return store.Initialize();
            });

            services.AddHealthChecks()
                .AddRavenDB(setup => { setup.Urls = Configuration.GetSection("Raven:Urls").Get<string[]>(); setup.Database = Configuration["Raven:Database"]; setup.Certificate = ravenDBcert; }, "ravendb");

            services.AddAuthorization();
            services.AddHttpContextAccessor();
            services.AddTransient<IAuthorizationPolicyProvider, AuthorizationPolicyProvider>();
            services.AddTransient<IAuthorizationHandler, RemoteAuthorizationHandler>();

            services.AddOpenApiDocument(config =>
            {
                config.DocumentName = "v1";
                config.PostProcess = document =>
                {
                    document.Info.Version = "v1";
                    document.Info.Title = "BeyondAuth Authorization Server";
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

            services.AddGrpc();
            services.AddControllers();

            services.AddOpenTelemetryTracing(
                (builder) => builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("beyondauth-authentication"))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    //.AddOtlpExporter(opt => opt.Endpoint = new Uri("grafana-agent:55680"))
                    .AddConsoleExporter()
                    );

            services.AddOpenTelemetryMetrics(builder =>
            {
                builder.AddAspNetCoreInstrumentation();
                builder.AddHttpClientInstrumentation();
            });

            services.AddSystemMetrics(registerDefaultCollectors: false);
            services.AddSystemMetricCollector<WindowsMemoryCollector>();
            services.AddSystemMetricCollector<LoadAverageCollector>();

            services.AddPrometheusCounters();
            services.AddPrometheusAspNetCoreMetrics();
            services.AddPrometheusHttpClientMetrics();
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
            app.UseExceptionHandler(new ExceptionHandlerOptions { AllowStatusCode404Response = true, ExceptionHandlingPath = "/error" });

            var forwardOptions = new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.All,
                ForwardedForHeaderName = "cf-connecting-ip",
                ForwardedHostHeaderName = "X-Forwarded-Host",
                ForwardedProtoHeaderName = "X-Forwarded-Proto",
            };
            forwardOptions.KnownNetworks.Clear();
            forwardOptions.KnownProxies.Clear();
            app.UseForwardedHeaders(forwardOptions);
            app.UseHttpsRedirection();

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
                    AppName = "authorizationserver",
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
                endpoints.MapGrpcService<AuthorizationService>();

                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Communication with gRPC endpoints must be made through a gRPC client. To learn how to create a client, visit: https://go.microsoft.com/fwlink/?linkid=2086909");
                });
                endpoints.MapMetrics();
            });
        }
    }
}
