using Autofac;
using Autofac.Configuration;
using CorrelationId.DependencyInjection;
using HealthChecks.UI.Client;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NSwag;
using NSwag.AspNetCore;
using NSwag.Generation.Processors.Security;
using Raven.Client.Documents;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;

namespace IdentityManager
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
                Configuration["ProxyNodes"]?.Split(';').ToList().ForEach(t =>
                {
                    if (!string.IsNullOrEmpty(t))
                        options.KnownProxies.Add(IPAddress.Parse(t));
                });
            });

            NLog.GlobalDiagnosticsContext.Set("AzureLogStorageConnectionString", Configuration["Azure:LogStorageConnectionString"]);

            services.AddDefaultCorrelationId(options =>
            {
                options.UpdateTraceIdentifier = true;
            });
            services.AddMemoryCache();

            services.AddMultiTenant<TenantSetting>().WithRouteStrategy("dataSourceId").WithStore(new ServiceLifetime(), (sp) => new RavenDBMultitenantStore(sp.GetService<IDocumentStore>(), sp.GetService<IMemoryCache>()))
                .WithPerTenantOptions<IdentityStoreOptions>((options, tenantInfo) =>
                {
                    options.DatabaseName = $"TenantIdentity-{tenantInfo.Identifier}";
                })
                .WithPerTenantOptions<UserStoreOptions>((options, tenantInfo) =>
                {
                    options.DatabaseName = $"TenantIdentity-{tenantInfo.Identifier}";
                });

            services.AddAuthorization(options =>
            {
                options.AddPolicy("ApiScope", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("scope", Configuration["Authentication:ApiName"]);
                });
            });

            services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                .AddIdentityServerAuthentication(x =>
                {
                    x.Authority = Configuration["Authentication:Authority"];
                    x.ApiSecret = Configuration["Authentication:ApiSecret"];
                    x.ApiName = Configuration["Authentication:ApiName"];
                    x.SupportedTokens = SupportedTokens.Both;
                    x.EnableCaching = true;
                    x.CacheDuration = TimeSpan.FromMinutes(1);
                });

            services.AddSingleton((ctx) =>
            {
                var certificateClient = new CertificateClient(vaultUri: new Uri(Environment.GetEnvironmentVariable("VaultUri")), credential: new DefaultAzureCredential());
                var secretClient = new SecretClient(new Uri(Environment.GetEnvironmentVariable("VaultUri")), new DefaultAzureCredential());

                var ravenDbCertificateClient = certificateClient.GetCertificate("RavenDB");
                var ravenDbCertificateSegments = ravenDbCertificateClient.Value.SecretId.Segments;
                var ravenDbCertificateBytes = Convert.FromBase64String(secretClient.GetSecret(ravenDbCertificateSegments[2].Trim('/'), ravenDbCertificateSegments[3].TrimEnd('/')).Value.Value);

                IDocumentStore store = new DocumentStore
                {
                    Urls = Configuration.GetSection("Raven:Urls").GetChildren().Select(t => t.Value).ToArray(),
                    Database = Configuration["Raven:Database"],
                    Certificate = new X509Certificate2(ravenDbCertificateBytes),
                    Conventions =
                    {
                        FindCollectionName = type =>
                        {
                            if (typeof(ApiResource).IsAssignableFrom(type))
                                return "ApiResources";
                            if (typeof(ApiScope).IsAssignableFrom(type))
                                return "ApiScopes";
                            if (typeof(Client).IsAssignableFrom(type))
                                return "Clients";
                            if (typeof(IdentityResource).IsAssignableFrom(type))
                                return "IdentityResources";
                            return DocumentConventions.DefaultGetCollectionName(type);
                        }
                    }
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

            services.ConfigureOptions<RavenOptionsSetup>();
            services.AddScoped(sp => sp.GetRequiredService<IDocumentStore>().OpenAsyncSession(sp.GetService<IOptions<UserStoreOptions>>()?.Value?.DatabaseName));

            services.AddScoped<IUserStore<ApplicationUser>, UserStore<ApplicationUser, Raven.Identity.IdentityRole>>();
            services.AddScoped<IRoleStore<Raven.Identity.IdentityRole>, RoleStore<Raven.Identity.IdentityRole>>();

            services.AddHealthChecks()
                .AddRavenDB(setup => { setup.Urls = new[] { Configuration["Raven:Url"] }; setup.Database = Configuration["Raven:Database"]; setup.Certificate = Configuration.GetSection("Raven:EncryptionEnabled").Get<bool>() ? new X509Certificate2(Configuration["Raven:CertFile"], Configuration["Raven:CertPassword"]) : null; }, "ravendb");

            services.AddOpenApiDocument(config =>
            {
                config.DocumentName = "v1";
                config.PostProcess = document =>
                {
                    document.Info.Version = "v1";
                    document.Info.Title = "BeyondAuth Identity Manager";
                    document.Info.Description = File.ReadAllText("readme.md");
                    document.Info.ExtensionData = new Dictionary<string, object>
                    {
                        { "x-logo", new { url = "/logo.png", altText = "BeyondAuth" } }
                    };
                    //document.ExtensionData.Add("x-tagGroups", new { name = "General", tags = new [] { "Discovery", "Tickets" }});

                    document.Tags.Add(new OpenApiTag { Name = "ApiResources", Description = "Api Resources" });
                    document.Tags.Add(new OpenApiTag { Name = "Clients", Description = "Clients" });
                    document.Tags.Add(new OpenApiTag { Name = "ClientsSecrets", Description = "Client Secrets" });
                    document.Tags.Add(new OpenApiTag { Name = "Users", Description = "Users" });

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
            services.AddApplicationInsightsTelemetry(Configuration["APPINSIGHTS_CONNECTIONSTRING"]);
        }

        ///// <summary>
        ///// ConfigureContainer is where you can register things directly
        ///// with Autofac. This runs after ConfigureServices so the things
        ///// here will override registrations made in ConfigureServices.
        ///// </summary>
        ///// <param name="builder"></param>
        //public void ConfigureContainer(ContainerBuilder builder)
        //{
        //    var module = new ConfigurationModule(Configuration);
        //    builder.RegisterModule(module);
        //}

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseForwardedHeaders();

            app.UseCors(x => x.AllowAnyOrigin().WithHeaders("accept", "authorization", "content-type", "origin").AllowAnyMethod());

            var policyCollection = new HeaderPolicyCollection()
                .AddFrameOptionsSameOrigin()
                .AddXssProtectionBlock()
                .AddContentTypeOptionsNoSniff()
                .AddStrictTransportSecurityMaxAgeIncludeSubDomainsAndPreload(maxAgeInSeconds: 60 * 60 * 24 * 365) // maxage = one year in seconds
                .AddReferrerPolicyStrictOriginWhenCrossOrigin()
                .RemoveServerHeader()
                //.AddContentSecurityPolicy(builder =>
                //{
                //    builder.AddObjectSrc().None();
                //    builder.AddFormAction().Self();
                //    builder.AddFrameAncestors().None();
                //})
                .AddFeaturePolicy(options =>
                {
                    options.AddAutoplay().Self();
                    options.AddCamera().Self();
                    options.AddFullscreen().Self();
                    options.AddGeolocation().Self();
                    options.AddMicrophone().Self();
                    options.AddPictureInPicture().Self();
                    options.AddSpeaker().Self();
                    options.AddSyncXHR().Self();
                })
                .AddPermissionsPolicy(options =>
                {
                    options.AddAutoplay().Self();
                    options.AddCamera().Self();
                    options.AddFullscreen().Self();
                    options.AddGeolocation().Self();
                    options.AddMicrophone().Self();
                    options.AddPictureInPicture().Self();
                    options.AddSpeaker().Self();
                    options.AddSyncXHR().Self();
                });
            app.UseSecurityHeaders(policyCollection);

            app.UseXContentTypeOptions();
            app.UseXDownloadOptions();
            app.UseXfo(options => options.SameOrigin());
            app.UseXXssProtection(options => options.EnabledWithBlockMode());
            app.UseHsts(options => options.MaxAge(30).AllResponses());

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
                    AppName = "identitymanager",
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
