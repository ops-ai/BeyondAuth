using Audit.Core;
using Audit.NET.RavenDB;
using Audit.NET.RavenDB.ConfigurationApi;
using Audit.NET.RavenDB.Providers;
using Autofac;
using Autofac.Configuration;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using BeyondAuth.PasswordValidators.Common;
using BeyondAuth.PasswordValidators.Topology;
using BeyondAuth.PolicyProvider;
using BlackstarSolar.AspNetCore.Identity.PwnedPasswords;
using CorrelationId;
using CorrelationId.DependencyInjection;
using HealthChecks.UI.Client;
using Identity.Core;
using IdentityServer4.AccessTokenValidation;
using IdentityServer4.Contrib.RavenDB.Options;
using IdentityServer4.Models;
using IdentityServer4.Stores.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
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
using Raven.Client.Documents.Conventions;
using Raven.Client.Json.Serialization.NewtonsoftJson;
using Raven.DependencyInjection;
using Raven.Identity;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json.Serialization;

namespace IdentityManager
{
    public class Startup
    {
        public Startup(IConfiguration configuration) => Configuration = configuration;

        public IConfiguration Configuration { get; }
        private DocumentStore auditStoreDb;

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

            services.Configure<IdentityOptions>(options =>
            {
                options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+ ";
                options.User.RequireUniqueEmail = true;

                // Default Lockout settings.
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequiredLength = 4;
            });

            var dataProtection = services.AddDataProtection().SetApplicationName(Configuration["DataProtection:AppName"]);

            services.AddAuthentication(IdentityServerAuthenticationDefaults.AuthenticationScheme)
                .AddJwtBearer();

            services.AddMultiTenant<TenantSetting>().WithBasePathStrategy().WithStore(new ServiceLifetime(), (sp) => new RavenDBMultitenantStore(sp.GetService<IDocumentStore>(), sp.GetService<IMemoryCache>()))
                .WithPerTenantOptions<IdentityStoreOptions>((options, tenantInfo) =>
                {
                    options.DatabaseName = $"TenantIdentity-{tenantInfo.Identifier}";
                })
                .WithPerTenantOptions<RavenSettings>((options, tenantInfo) =>
                {
                    options.DatabaseName = $"TenantIdentity-{tenantInfo.Identifier}";
                })
                .WithPerTenantOptions<IdentityOptions>((options, tenantInfo) =>
                {
                    options.Password.RequireDigit = tenantInfo.IdentityOptions.Password.RequireDigit;
                    options.Password.RequiredLength = tenantInfo.IdentityOptions.Password.RequiredLength;
                    options.Password.RequiredUniqueChars = tenantInfo.IdentityOptions.Password.RequiredUniqueChars;
                    options.Password.RequireLowercase = tenantInfo.IdentityOptions.Password.RequireLowercase;
                    options.Password.RequireNonAlphanumeric = tenantInfo.IdentityOptions.Password.RequireNonAlphanumeric;
                    options.Password.RequireUppercase = tenantInfo.IdentityOptions.Password.RequireUppercase;
                    options.Lockout.DefaultLockoutTimeSpan = tenantInfo.IdentityOptions.Lockout.DefaultLockoutTimeSpan;
                    options.Lockout.AllowedForNewUsers = tenantInfo.IdentityOptions.Lockout.AllowedForNewUsers;
                    options.Lockout.MaxFailedAccessAttempts = tenantInfo.IdentityOptions.Lockout.MaxFailedAccessAttempts;
                    options.User.AllowedUserNameCharacters = tenantInfo.IdentityOptions.User.AllowedUserNameCharacters;
                    options.User.RequireUniqueEmail = tenantInfo.IdentityOptions.User.RequireUniqueEmail;
                    options.SignIn.RequireConfirmedEmail = tenantInfo.IdentityOptions.SignIn.RequireConfirmedEmail;
                    options.SignIn.RequireConfirmedPhoneNumber = tenantInfo.IdentityOptions.SignIn.RequireConfirmedPhoneNumber;
                    options.SignIn.RequireConfirmedAccount = tenantInfo.IdentityOptions.SignIn.RequireConfirmedAccount;
                })
                .WithPerTenantOptions<PasswordTopologyValidatorOptions>((options, tenantInfo) =>
                {
                    options.RollingHistoryInMonths = 5;
                    options.Threshold = 1000;
                })
                .WithPerTenantOptions<PwnedPasswordsValidatorOptions>((options, tenantInfo) =>
                {
                    options.ApiKey = Configuration["HaveIBeenPwned:ApiKey"];
                })
                .WithPerTenantOptions<JwtBearerOptions>((options, tenantInfo) =>
                {
                    options.Authority = $"https://{tenantInfo.Identifier}";
                    options.TokenValidationParameters.ValidIssuers = new[] { $"https://{tenantInfo.Identifier}", Configuration["Authentication:Authority"] };
                    options.Audience = tenantInfo.IdpSettings.ApiName;


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
                        document.Info.Title = "BeyondAuth Identity Manager";
                        document.Info.Description = File.ReadAllText("readme.md");
                        document.Info.ExtensionData = new Dictionary<string, object>
                    {
                        { "x-logo", new { url = "/logo.png", altText = "BeyondAuth" } }
                    };
                        document.ExtensionData.Add("x-tagGroups", new { name = "OAuth2 / OpenID Connect", tags = new[] { "ApiResources", "ApiResourceSecrets", "Clients", "ClientSecrets" } });
                        //document.ExtensionData.Add("x-tagGroups", new { name = "Users", tags = new [] { "Users" } });

                        document.Tags = document.Tags.OrderBy(t => t.Name).ToList();

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

            //services.AddOptions().AddLogging();
            //// Services used by identity
            //services.TryAddScoped<IUserValidator<TUser>, UserValidator<TUser>>();
            services.TryAddScoped<IPasswordValidator<ApplicationUser>, PasswordValidator<ApplicationUser>>();
            //services.TryAddScoped<IPasswordHasher<TUser>, PasswordHasher<TUser>>();
            //services.TryAddScoped<ILookupNormalizer, UpperInvariantLookupNormalizer>();
            //// No interface for the error describer so we can add errors without rev'ing the interface
            //services.TryAddScoped<IdentityErrorDescriber>();
            //services.TryAddScoped<IUserClaimsPrincipalFactory<TUser>, UserClaimsPrincipalFactory<TUser>>();
            //services.TryAddScoped<UserManager<TUser>>();

            var identityBuilder = services.AddIdentityCore<ApplicationUser>()
                .AddDefaultTokenProviders()
                .AddPasswordValidator<EmailAsPasswordValidator<ApplicationUser>>()
                .AddPasswordValidator<InvalidPhrasePasswordValidator<ApplicationUser>>()
                .AddPwnedPasswordsValidator<ApplicationUser>()
                .AddTop1000PasswordValidator<ApplicationUser>()
                .AddPasswordValidator<PasswordTopologyValidator<ApplicationUser>>();

            services.AddAuthorization(options =>
            {
                options.AddPolicy("ApiScope", policy =>
                {
                    policy.RequireAuthenticatedUser();
                    policy.RequireClaim("scope", Configuration["Authentication:ApiName"]);
                });
            });

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
                IDocumentStore store = new DocumentStore
                {
                    Urls = Configuration.GetSection("Raven:Urls").Get<string[]>(),
                    Database = Configuration["Raven:Database"],
                    Certificate = ravenDBcert,
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
            services.AddScoped(sp => sp.GetRequiredService<IDocumentStore>().OpenAsyncSession(sp.GetService<IOptions<RavenSettings>>()?.Value?.DatabaseName));

            auditStoreDb = new DocumentStore { Urls = Configuration.GetSection("Raven:Urls").Get<string[]>(), Certificate = ravenDBcert, Database = Configuration["Raven:DatabaseName"] };
            var serializer = new NewtonsoftJsonSerializationConventions
            {
                JsonContractResolver = new AuditContractResolver()
            };
            serializer.CustomizeJsonSerializer += (JsonSerializer serializer) =>
            {
                serializer.DefaultValueHandling = DefaultValueHandling.IgnoreAndPopulate;
                serializer.NullValueHandling = NullValueHandling.Ignore;
            };
            auditStoreDb.Conventions.Serialization = serializer;
            auditStoreDb.Initialize();

            services.AddScoped<IUserStore<ApplicationUser>, UserStore<ApplicationUser, Raven.Identity.IdentityRole>>();
            services.AddScoped<IRoleStore<Raven.Identity.IdentityRole>, RoleStore<Raven.Identity.IdentityRole>>();
            services.AddTransient<IPasswordTopologyProvider, PasswordTopologyProvider>();
            services.AddSingleton<IAuthorizationHandler, AclAuthorizationHandler>();

            services.AddHealthChecks()
                .AddRavenDB(setup => { setup.Urls = Configuration.GetSection("Raven:Urls").Get<string[]>(); setup.Database = Configuration["Raven:Database"]; setup.Certificate = ravenDBcert; }, "ravendb");

            services.AddOpenApiDocument(config =>
            {

            });

            services.AddMvc()
                .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

            services.AddControllers().AddNewtonsoftJson();
            services.AddApplicationInsightsTelemetry(Configuration["APPINSIGHTS_CONNECTIONSTRING"]);

            services.AddOpenTelemetryTracing(
                (builder) => builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("beyondauth-idp"))
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
                IdentityModelEventSource.ShowPII = true;
            }

            app.UseHttpsRedirection();

            app.UseForwardedHeaders();

            var auditConfig = Audit.Core.Configuration.Setup().UseFactory(
                () =>
                new RavenDbDataProvider(auditStoreDb, app.ApplicationServices.GetRequiredService<IOptions<IdentityStoreOptions>>().Value.DatabaseName, storeDiffOnly: true));

            Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaving, scope =>
            {
                var httpContextAccessor = app.ApplicationServices.GetService<IHttpContextAccessor>();
                if (httpContextAccessor?.HttpContext?.User?.Identity?.Name != null)
                    scope.SetCustomField("Username", httpContextAccessor.HttpContext?.User?.Identity?.Name);
            });

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
                options.EnableTryItOut = true;
                options.OAuth2Client = new OAuth2ClientSettings
                {
                    ClientId = Configuration["Authentication:ClientId"],
                    AppName = "identitymanager",
                    ClientSecret = Configuration["Authentication:ClientSecret"],
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

            app.UseCorrelationId();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers().RequireAuthorization("ApiScope");
                endpoints.MapMetrics();
            });
        }
    }
}
