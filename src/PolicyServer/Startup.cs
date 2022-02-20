using Autofac;
using Autofac.Configuration;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using CorrelationId;
using CorrelationId.DependencyInjection;
using HealthChecks.UI.Client;
using Identity.Core;
using IdentityServer4.AccessTokenValidation;
using IdentityServer4.Contrib.RavenDB.Options;
using IdentityServer4.Stores.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.Caching.Memory;
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
using Raven.DependencyInjection;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;

namespace PolicyServer
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

            services.AddDefaultCorrelationId(options =>
            {
                options.UpdateTraceIdentifier = true;
            });
            services.AddMemoryCache();

            services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                .AddJwtBearer();

            services.AddMultiTenant<TenantSetting>()
                .WithDelegateStrategy(context =>
                {
                    if (context is not HttpContext httpContext)
                        throw new ArgumentException("{context} is not HttpContext", nameof(context));

                    if (!System.Net.Http.Headers.AuthenticationHeaderValue.TryParse(httpContext.Request.Headers.Authorization, out var parsedValue))
                        throw new ArgumentException("Invalid auth header");

                    return Task.FromResult(new JwtSecurityToken(parsedValue.Parameter).Issuer[8..]);

                }).WithStore(new ServiceLifetime(), (sp) => new RavenDBMultitenantStore(sp.GetService<IDocumentStore>(), sp.GetService<IMemoryCache>()))
                .WithPerTenantOptions<IdentityStoreOptions>((options, tenantInfo) =>
                {
                    options.DatabaseName = $"TenantIdentity-{tenantInfo.Identifier}";
                })
                .WithPerTenantOptions<RavenSettings>((options, tenantInfo) =>
                {
                    options.DatabaseName = $"TenantIdentity-{tenantInfo.Identifier}";
                })
                .WithPerTenantOptions<JwtBearerOptions>((options, tenantInfo) =>
                {
                    options.Authority = $"https://{tenantInfo.Identifier}";
                    options.TokenValidationParameters.ValidIssuers = new[] { $"https://{tenantInfo.Identifier}" };
                    options.Audience = tenantInfo.PolicyServerSettings.ApiName;

                    //options.ApiSecret = tenantInfo.IdpSettings.ApiSecret;
                    options.RequireHttpsMetadata = true;
                    //options.SupportedTokens = SupportedTokens.Both;
                    //options.EnableCaching = true;
                    //options.Validate();
                    //options.CacheDuration = TimeSpan.FromMinutes(1);
                })
                .WithPerTenantOptions<NSwag.Generation.AspNetCore.AspNetCoreOpenApiDocumentGeneratorSettings>((config, tenantInfo) =>
                {
                    config.DocumentName = "v1";
                    config.PostProcess = document =>
                    {
                        document.Info.Version = "v1";
                        document.Info.Title = "BeyondAuth Policy Server";
                        document.Info.Description = File.ReadAllText("readme.md");
                        document.Info.ExtensionData = new Dictionary<string, object>
                    {
                        { "x-logo", new { url = "/logo.png", altText = "BeyondAuth" } }
                    };
                        document.Info.Contact = new OpenApiContact
                        {
                            Name = Configuration["Support:Name"],
                            Email = Configuration["Support:Email"],
                            Url = Configuration["Support:Link"]
                        };
                    };

                    config.AddSecurity("bearer", Enumerable.Empty<string>(), new OpenApiSecurityScheme
                    {
                        Type = OpenApiSecuritySchemeType.OAuth2,
                        Description = "Auth",
                        Flow = OpenApiOAuth2Flow.AccessCode,
                        OpenIdConnectUrl = new Uri(new Uri($"https://{tenantInfo.Identifier}"), ".well-known/openid-configuration").AbsoluteUri,
                        Flows = new OpenApiOAuthFlows
                        {
                            AuthorizationCode = new OpenApiOAuthFlow
                            {
                                AuthorizationUrl = new Uri(new Uri($"https://{tenantInfo.Identifier}"), "connect/authorize").AbsoluteUri,
                                TokenUrl = new Uri(new Uri($"https://{tenantInfo.Identifier}"), "connect/token").AbsoluteUri,
                                Scopes = new Dictionary<string, string> { { "openid", "openid" }, { tenantInfo.IdpSettings.ApiName, tenantInfo.IdpSettings.ApiName } }
                            }
                        }
                    });
                    config.OperationProcessors.Add(new AspNetCoreOperationSecurityScopeProcessor("bearer"));

                    config.SerializerSettings = new JsonSerializerSettings
                    {
                        ContractResolver = new CamelCasePropertyNamesContractResolver(),
                    };
                    config.SerializerSettings.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());
                })
                .WithPerTenantAuthentication();

            X509Certificate2? ravenDBcert = null;
            if (Environment.GetEnvironmentVariable("VaultUri") != null)
            {
                var certificateClient = new CertificateClient(vaultUri: new Uri(Environment.GetEnvironmentVariable("VaultUri")!), credential: new DefaultAzureCredential());
                var secretClient = new SecretClient(new Uri(Environment.GetEnvironmentVariable("VaultUri")!), new DefaultAzureCredential());

                var ravenDbCertificateClient = certificateClient.GetCertificate("RavenDB");
                var ravenDbCertificateSegments = ravenDbCertificateClient.Value.SecretId.Segments;
                var ravenDbCertificateBytes = Convert.FromBase64String(secretClient.GetSecret(ravenDbCertificateSegments[2].Trim('/'), ravenDbCertificateSegments[3].TrimEnd('/')).Value.Value);
                ravenDBcert = new X509Certificate2(ravenDbCertificateBytes);
            }

            services.AddSingleton((ctx) =>
            {
                var store = new DocumentStore
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

            services.AddOpenApiDocument(config =>
            {
                config.DocumentName = "v1";
                config.PostProcess = document =>
                {
                    document.Info.Version = "v1";
                    document.Info.Title = "BeyondAuth Policy Server";
                    document.Info.Description = File.ReadAllText("readme.md");
                    document.Info.ExtensionData = new Dictionary<string, object>
                    {
                        { "x-logo", new { url = "/logo.png", altText = "BeyondAuth" } }
                    };

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

            services.AddMvc()
                .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

            services.AddControllers().AddNewtonsoftJson();

            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser().Build();
            });

            services.AddOpenTelemetryTracing(
                (builder) => builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("beyondauth-policy"))
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
            if (env.IsDevelopment())
                app.UseDeveloperExceptionPage();

            app.UseHttpsRedirection();
            app.UseForwardedHeaders();

            app.UseCors(x => x.AllowAnyOrigin().WithHeaders("accept", "authorization", "content-type", "origin").AllowAnyMethod());

            app.UseXContentTypeOptions();
            app.UseXDownloadOptions();
            app.UseXfo(options => options.SameOrigin());
            app.UseXXssProtection(options => options.EnabledWithBlockMode());
            app.UseHsts(options => options.MaxAge(30).AllResponses());
            app.UseCorrelationId();

            app.UseRouting();
            app.UseMultiTenant();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseOpenApi();
            app.UseSwaggerUi3(options =>
            {
                options.OAuth2Client = new OAuth2ClientSettings
                {
                    ClientId = "swagger",
                    AppName = "policyserver",
                    UsePkceWithAuthorizationCodeGrant = true,
                };
            });

            app.UseHealthChecks(Configuration["HealthChecks:FullEndpoint"], new HealthCheckOptions
            {
                Predicate = _ => true,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            app.UseHealthChecks(Configuration["HealthChecks:SummaryEndpoint"], new HealthCheckOptions
            {
                Predicate = _ => _.FailureStatus == HealthStatus.Unhealthy
            });

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
                endpoints.MapMetrics();
            });
        }
    }
}
