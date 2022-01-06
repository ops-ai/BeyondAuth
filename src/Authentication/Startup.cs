using Authentication.Extensions;
using Authentication.Options;
using Autofac;
using Azure.Identity;
using CorrelationId.DependencyInjection;
using HealthChecks.UI.Client;
using Identity.Core;
using IdentityServer4.Contrib.RavenDB.Options;
using IdentityServer4.Contrib.RavenDB.Services;
using IdentityServer4.Contrib.RavenDB.Stores;
using IdentityServer4.Stores.Serialization;
using JSNLog;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Raven.Client.Documents;
using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Raven.Client.Json.Serialization.NewtonsoftJson;
using IdentityServer4;
using IdentityServer4.AspNetIdentity;
using System.Text;
using System.Net.Http.Headers;
using System.Net.Http;
using Microsoft.AspNetCore.Rewrite;
using CorrelationId;
using Authentication.Infrastructure;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Authentication.Controllers;
using Polly;
using Raven.Identity;
using Raven.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.OAuth;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using Raven.Client.Documents.Conventions;
using IdentityServer4.Models;
using Microsoft.AspNetCore.HttpOverrides;
using System.Collections.Generic;
using NetTools;
using BeyondAuth.PasswordValidators.Common;
using BlackstarSolar.AspNetCore.Identity.PwnedPasswords;
using BeyondAuth.PasswordValidators.Topology;
using OpenTelemetry.Trace;
using OpenTelemetry.Resources;
using IdentityServer4.Services;
using System.Diagnostics;
using OpenTelemetry;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Raven.Client.Documents.Linq;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using Identity.Core.Settings;
using OpenTelemetry.Metrics;
using Toggly.FeatureManagement;
using Toggly.FeatureManagement.Storage.RavenDB;

namespace Authentication
{
    public class Startup
    {
        public Startup(IConfiguration configuration) => Configuration = configuration;

        public IConfiguration Configuration { get; }

        public ILifetimeScope AutofacContainer { get; private set; }

        /// <summary>
        /// This method gets called by the runtime. Use this method to add services to the container.
        /// </summary>
        /// <param name="services"></param>
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddOptions();

            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardLimit = 2;
                //options.ForwardedForHeaderName = Configuration["Proxy:HeaderName"];
                options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                //options.KnownNetworks.Clear();
                //options.KnownProxies.Clear();
            });
            services.AddCertificateForwarding(options => options.CertificateHeader = "X-ARR-ClientCert");
            services.Configure<TogglySettings>(Configuration.GetSection("Toggly"));
            services.AddSingleton<IFeatureDefinitionProvider, TogglyFeatureProvider>();
            services.AddSingleton<IFeatureSnapshotProvider, RavenDBFeatureSnapshotProvider>();

            services.AddHttpClient();
            services.AddHttpClient("toggly", config =>
            {
                config.BaseAddress = new Uri(Configuration["Toggly:BaseUrl"]);
            })
                .ConfigurePrimaryHttpMessageHandler(() => { return new SocketsHttpHandler { UseCookies = false }; });

            services.AddSingleton<ITargetingContextAccessor, HttpContextTargetingContextAccessor>();
            services.AddFeatureManagement()
                    .AddFeatureFilter<PercentageFilter>()
                    .AddFeatureFilter<ContextualTargetingFilter>()
                    .AddFeatureFilter<TargetingFilter>()
                    .AddFeatureFilter<TimeWindowFilter>();

            NLog.GlobalDiagnosticsContext.Set("AzureLogStorageConnectionString", Configuration["LogStorage:AzureStorage"]);
            NLog.GlobalDiagnosticsContext.Set("LokiConnectionString", Configuration["LogStorage:Loki:Url"]);
            NLog.GlobalDiagnosticsContext.Set("LokiUsername", Configuration["LogStorage:Loki:Username"]);
            NLog.GlobalDiagnosticsContext.Set("LokiPassword", Configuration["LogStorage:Loki:Password"]);
            NLog.GlobalDiagnosticsContext.Set("AppName", Configuration["DataProtection:AppName"]);

            services.AddDefaultCorrelationId(options =>
            {
                options.UpdateTraceIdentifier = true;
            });

            services.AddAntiforgery(options =>
            {
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.HttpOnly = true;
                options.Cookie.SameSite = SameSiteMode.Strict;
            });

            services.AddResponseCaching(options =>
            {
                options.UseCaseSensitivePaths = false;
            });
            services.AddMemoryCache();

            var dataProtection = services.AddDataProtection().SetApplicationName(Configuration["DataProtection:AppName"]);

            if (!string.IsNullOrEmpty(Configuration["DataProtection:KeyIdentifier"]))
            {
                var blobClient = new BlobClient(Configuration["DataProtection:StorageConnectionString"], Configuration["DataProtection:StorageContainer"], "keys.xml");

                dataProtection
                    .ProtectKeysWithAzureKeyVault(new Uri(Configuration["DataProtection:KeyIdentifier"]), new DefaultAzureCredential())
                    .PersistKeysToAzureBlobStorage(blobClient);
            }

            services.AddAuthorization();

            services.AddDistributedMemoryCache();
            services.AddOidcStateDataFormatterCache();

            var authenticationServices = services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme);

            services.AddMultiTenant<TenantSetting>().WithHostStrategy("__tenant__").WithStore(new ServiceLifetime(), (sp) => new RavenDBMultitenantStore(sp.GetService<IDocumentStore>(), sp.GetService<IMemoryCache>()))
                .WithPerTenantOptions<AccountOptions>((options, tenantInfo) =>
                {
                    options.AllowLocalLogin = tenantInfo.AccountOptions.AllowLocalLogin;
                    options.AllowRememberLogin = tenantInfo.AccountOptions.AllowRememberLogin;
                    options.AllowPasswordReset = tenantInfo.AccountOptions.AllowPasswordReset;
                    options.AutomaticRedirectAfterSignOut = tenantInfo.AccountOptions.AutomaticRedirectAfterSignOut;
                    options.DashboardUrl = tenantInfo.AccountOptions.DashboardUrl;
                    options.DefaultDomain = tenantInfo.AccountOptions.DefaultDomain;
                    options.IncludeWindowsGroups = tenantInfo.AccountOptions.IncludeWindowsGroups;
                    options.InvalidCredentialsErrorMessage = tenantInfo.AccountOptions.InvalidCredentialsErrorMessage;
                    options.RememberMeLoginDuration = tenantInfo.AccountOptions.RememberMeLoginDuration;
                    options.ShowLogoutPrompt = tenantInfo.AccountOptions.ShowLogoutPrompt;
                    options.SupportEmail = tenantInfo.AccountOptions.SupportEmail;
                    options.SupportLink = tenantInfo.AccountOptions.SupportLink;
                    options.WindowsAuthenticationSchemeName = tenantInfo.AccountOptions.WindowsAuthenticationSchemeName;
                })
                .WithPerTenantOptions<ConsentOptions>((options, tenantInfo) =>
                {
                    options.EnableOfflineAccess = tenantInfo.ConsentOptions.EnableOfflineAccess;
                    options.InvalidSelectionErrorMessage = tenantInfo.ConsentOptions.InvalidSelectionErrorMessage;
                    options.MustChooseOneErrorMessage = tenantInfo.ConsentOptions.MustChooseOneErrorMessage;
                    options.OfflineAccessDescription = tenantInfo.ConsentOptions.OfflineAccessDescription;
                    options.OfflineAccessDisplayName = tenantInfo.ConsentOptions.OfflineAccessDisplayName;
                })
                .WithPerTenantOptions<AuthenticationOptions>((options, tenantInfo) =>
                {
                    //options.DefaultChallengeScheme = ;
                })
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
                    options.Password = tenantInfo.IdentityOptions.Password;
                    options.Lockout = tenantInfo.IdentityOptions.Lockout;
                    options.User = tenantInfo.IdentityOptions.User;
                    options.SignIn = tenantInfo.IdentityOptions.SignIn;
                })
                .WithPerTenantOptions<EmailOptions>((options, tenantInfo) =>
                {
                    if (tenantInfo.EmailSettings.From != null)
                        options.From = tenantInfo.EmailSettings.From;
                    if (tenantInfo.EmailSettings.ReplyTo != null)
                        options.ReplyTo = tenantInfo.EmailSettings.ReplyTo;
                    if (tenantInfo.EmailSettings.DisplayName != null)
                        options.DisplayName = tenantInfo.EmailSettings.DisplayName;
                    if (tenantInfo.EmailSettings.SupportEmail != null)
                        options.SupportEmail = tenantInfo.EmailSettings.SupportEmail;
                    if (tenantInfo.EmailSettings.SendingKey != null)
                        options.SendingKey = tenantInfo.EmailSettings.SendingKey;
                    if (tenantInfo.EmailSettings.PrivateKey != null)
                        options.PrivateKey = tenantInfo.EmailSettings.PrivateKey;
                    if (tenantInfo.EmailSettings.ApiBaseUrl != null)
                        options.ApiBaseUrl = tenantInfo.EmailSettings.ApiBaseUrl;
                })
                .WithPerTenantOptions<SmsOptions>((options, tenantInfo) =>
                {
                    if (tenantInfo.SmsSettings.SmsAccountFrom != null)
                        options.SmsAccountFrom = tenantInfo.SmsSettings.SmsAccountFrom;
                    if (tenantInfo.SmsSettings.SmsAccountIdentification != null)
                        options.SmsAccountIdentification = tenantInfo.SmsSettings.SmsAccountIdentification;
                    if (tenantInfo.SmsSettings.SmsAccountPassword != null)
                        options.SmsAccountPassword = tenantInfo.SmsSettings.SmsAccountPassword;
                })
                .WithPerTenantOptions<GoogleCaptchaOptions>((options, tenantInfo) =>
                {
                    if (tenantInfo.GoogleCaptcha.Secret != null)
                        options.Secret = tenantInfo.GoogleCaptcha.Secret;
                    if (tenantInfo.GoogleCaptcha.SiteKey != null)
                        options.SiteKey = tenantInfo.GoogleCaptcha.SiteKey;
                })
                .WithPerTenantOptions<PasswordTopologyValidatorOptions>((options, tenantInfo) =>
                {
                    options.RollingHistoryInMonths = 5;
                    options.Threshold = 1000;
                })
                .WithPerTenantOptions<CookieAuthenticationOptions>((o, tenantInfo) =>
                {
                    o.LoginPath = "/login";
                    o.LogoutPath = "/logout";
                    o.Cookie.Name += tenantInfo.Id;
                })
                .WithPerTenantOptions<GoogleOptions>((o, tenantInfo) =>
                {
                    if (!tenantInfo.ExternalIdps.Any(t => t.Name.Equals("Google") && t.Enabled))
                        return;

                    o.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;

                    // register your IdentityServer with Google at https://console.developers.google.com
                    // enable the Google+ API
                    // set the redirect URI to https://localhost:5001/signin-google

                    var googleSettings = tenantInfo.ExternalIdps.First(t => t.Name == "Google") as ExternalOidcIdentityProvider;

                    o.ClientId = googleSettings.ClientId;
                    o.ClientSecret = googleSettings.ClientSecret;

                    o.AuthorizationEndpoint += "?prompt=consent"; // Hack so we always get a refresh token, it only comes on the first authorization response
                    o.AccessType = "offline";
                    o.SaveTokens = true;
                    o.Events = new OAuthEvents()
                    {
                        OnRemoteFailure = HandleOnRemoteFailure
                    };
                    o.ClaimActions.MapJsonSubKey("urn:google:image", "image", "url");
                    o.ClaimActions.Remove(ClaimTypes.GivenName);
                })
                .WithPerTenantOptions<FacebookOptions>((o, tenantInfo) =>
                {
                    if (!tenantInfo.ExternalIdps.Any(t => t.Name.Equals("Facebook") && t.Enabled))
                        return;

                    o.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;

                    // You must first create an app with Facebook and add its ID and Secret to your user-secrets.
                    // https://developers.facebook.com/apps/
                    // https://developers.facebook.com/docs/facebook-login/manually-build-a-login-flow#login

                    var facebookSettings = tenantInfo.ExternalIdps.First(t => t.Name == "Facebook") as ExternalOidcIdentityProvider;

                    o.AppId = facebookSettings.ClientId; //appid
                    o.AppSecret = facebookSettings.ClientSecret; //appsecret
                    o.Scope.Add("email");
                    o.Fields.Add("name");
                    o.Fields.Add("email");
                    o.SaveTokens = true;
                    o.Events = new OAuthEvents()
                    {
                        OnRemoteFailure = HandleOnRemoteFailure
                    };
                })
                .WithPerTenantOptions<TwitterOptions>((o, tenantInfo) =>
                {
                    if (!tenantInfo.ExternalIdps.Any(t => t.Name.Equals("Twitter") && t.Enabled))
                        return;

                    o.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;

                    // You must first create an app with Twitter and add its key and Secret to your user-secrets.
                    // https://apps.twitter.com/
                    // https://developer.twitter.com/en/docs/basics/authentication/api-reference/access_token

                    var twitterSettings = tenantInfo.ExternalIdps.First(t => t.Name == "Twitter") as ExternalOidcIdentityProvider;

                    o.ConsumerKey = twitterSettings.ClientId; //consumerkey
                    o.ConsumerSecret = twitterSettings.ClientSecret; //consumersecret
                    // http://stackoverflow.com/questions/22627083/can-we-get-email-id-from-twitter-oauth-api/32852370#32852370
                    // http://stackoverflow.com/questions/36330675/get-users-email-from-twitter-api-for-external-login-authentication-asp-net-mvc?lq=1
                    o.RetrieveUserDetails = true;
                    o.SaveTokens = true;
                    o.ClaimActions.MapJsonKey("urn:twitter:profilepicture", "profile_image_url", ClaimTypes.Uri);
                    o.Events = new TwitterEvents()
                    {
                        OnRemoteFailure = HandleOnRemoteFailure
                    };
                })
                .WithPerTenantOptions<MicrosoftAccountOptions>((o, tenantInfo) =>
                {
                    if (!tenantInfo.ExternalIdps.Any(t => t.Name.Equals("MicrosoftAccount") && t.Enabled))
                        return;

                    o.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;

                    // You must first create an app with Microsoft Account and add its ID and Secret to your user-secrets.
                    // https://azure.microsoft.com/en-us/documentation/articles/active-directory-v2-app-registration/
                    // https://apps.dev.microsoft.com/

                    var microsoftSettings = tenantInfo.ExternalIdps.First(t => t.Name == "MicrosoftAccount") as ExternalOidcIdentityProvider;

                    o.ClientId = microsoftSettings.ClientId;
                    o.ClientSecret = microsoftSettings.ClientSecret;
                    o.SaveTokens = true;
                    o.Scope.Add("offline_access");
                    o.Events = new OAuthEvents()
                    {
                        OnRemoteFailure = HandleOnRemoteFailure
                    };
                })
                .WithPerTenantOptions<OpenIdConnectOptions>((o, tenantInfo) =>
                {
                    if (!tenantInfo.ExternalIdps.Any(t => !t.Name.In("MicrosoftAccount", "Twitter", "Facebook", "Google") && t.Enabled))
                    {
                        o.ClientId = "test";
                        o.ClientSecret = "test";
                        return;
                    }

                    o.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;

                    // You must first create an app with Microsoft Account and add its ID and Secret to your user-secrets.
                    // https://azure.microsoft.com/en-us/documentation/articles/active-directory-v2-app-registration/
                    // https://apps.dev.microsoft.com/

                    var microsoftSettings = tenantInfo.ExternalIdps.First(t => t.Name == "MicrosoftAccount") as ExternalOidcIdentityProvider;

                    o.ClientId = microsoftSettings.ClientId;
                    o.ClientSecret = microsoftSettings.ClientSecret;
                    o.SaveTokens = true;
                    o.Scope.Add("offline_access");
                    o.Events = new OpenIdConnectEvents()
                    {
                        OnRemoteFailure = HandleOnRemoteFailure
                    };
                })
                .WithPerTenantAuthentication();

            services.AddControllersWithViews();
            var razorBuilder = services.AddRazorPages();
#if DEBUG
            razorBuilder.AddRazorRuntimeCompilation();
#endif
            services.AddSameSiteCookiePolicy();

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
            services.AddScoped(sp => sp.GetRequiredService<IDocumentStore>().OpenAsyncSession(sp.GetService<IOptions<RavenSettings>>()?.Value?.DatabaseName));

            var identityBuilder = services.AddIdentity<ApplicationUser, Raven.Identity.IdentityRole>(options => { })
                .AddDefaultTokenProviders()
                .AddPasswordValidator<EmailAsPasswordValidator<ApplicationUser>>()
                .AddPasswordValidator<InvalidPhrasePasswordValidator<ApplicationUser>>()
                .AddPwnedPasswordsValidator<ApplicationUser>(options => options.ApiKey = Configuration["HaveIBeenPwned:ApiKey"])
                .AddTop1000PasswordValidator<ApplicationUser>()
                .AddPasswordValidator<PasswordTopologyValidator<ApplicationUser>>();

            identityBuilder.Services.AddScoped<IUserStore<ApplicationUser>, UserStore<ApplicationUser, Raven.Identity.IdentityRole>>();
            identityBuilder.Services.AddScoped<IRoleStore<Raven.Identity.IdentityRole>, RoleStore<Raven.Identity.IdentityRole>>();

            var healthChecks = services.AddHealthChecks()
                //.AddRavenDB(setup => { setup.Urls = Configuration.GetSection("Raven:Urls").GetChildren().Select(t => t.Value).ToArray(); setup.Database = Configuration["Raven:Database"]; setup.Certificate = new X509Certificate2(ravenDbCertificateBytes); }, "ravendb")
                .AddIdentityServer(new Uri(Configuration["BaseUrl"]), "openid-connect");

            if (Environment.GetEnvironmentVariable("VaultUri") != null)
                healthChecks.AddAzureKeyVault(new Uri(Environment.GetEnvironmentVariable("VaultUri")), new DefaultAzureCredential(), options =>
                {

                });

            services.AddHttpClient("mailgun", config =>
            {
                config.BaseAddress = new Uri(Configuration["EmailSettings:ApiBaseUrl"]);
                var authToken = Encoding.ASCII.GetBytes($"api:{Configuration["EmailSettings:SendingKey"]}");
                config.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(authToken));
            })
                .ConfigurePrimaryHttpMessageHandler(() => { return new SocketsHttpHandler { UseCookies = false }; })
                .AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(20, retryAttempt => TimeSpan.FromMilliseconds(300 * retryAttempt)));

            services.AddHttpClient("captcha", config =>
            {
                config.BaseAddress = new Uri("https://www.google.com/");
            })
                .ConfigurePrimaryHttpMessageHandler(() => { return new SocketsHttpHandler { UseCookies = false }; })
                .AddTransientHttpErrorPolicy(p => p.WaitAndRetryAsync(20, retryAttempt => TimeSpan.FromMilliseconds(300 * retryAttempt)));

            //services.AddTransient<IRedirectUriValidator, RedirectUriValidator>();

            authenticationServices.AddCertificate("Certificate", options =>
             {
                 // allows both self-signed and CA-based certs. Check the MTLS spec for details.
                 options.AllowedCertificateTypes = CertificateTypes.All;
             }).AddCookie(options =>
             {
                 options.Cookie.Name = "BA.";
                 options.LoginPath = "/login";
                 options.LogoutPath = "/logout";
             });
            authenticationServices.AddTwoFactorUserIdCookie();
            authenticationServices.AddTwoFactorRememberMeCookie();
            //.AddOpenIdConnect()
            //.AddGoogle();
            //.AddFacebook()
            //.AddTwitter()
            //.AddMicrosoftAccount();

            var builder = services.AddIdentityServer(options =>
            {
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;

                options.UserInteraction.LoginUrl = "/login";
                options.UserInteraction.LogoutUrl = "/logout";

                options.MutualTls.Enabled = true;
                options.MutualTls.ClientCertificateAuthenticationScheme = "Certificate";
            })
                .AddPersistedGrantStore<RavenDBPersistedGrantStore>()
                .AddClientStore<RavenDBClientStore>()
                .AddResourceStore<RavenDBResourceStore>()
                .AddCorsPolicyService<CorsPolicyService>()
                .AddAspNetIdentity<ApplicationUser>()
                .AddResourceOwnerValidator<ResourceOwnerPasswordValidator<ApplicationUser>>()
                .AddProfileService<ProfileService<ApplicationUser>>();

            try
            {
                var certificateClient = new CertificateClient(vaultUri: new Uri(Environment.GetEnvironmentVariable("VaultUri")), credential: new DefaultAzureCredential());
                var secretClient = new SecretClient(new Uri(Environment.GetEnvironmentVariable("VaultUri")), new DefaultAzureCredential());

                var idpSigningCertificateClient = certificateClient.GetCertificate("IdentitySigning");
                var idpSigningCertificateSegments = idpSigningCertificateClient.Value.SecretId.Segments;
                var idpSigningCertificateBytes = Convert.FromBase64String(secretClient.GetSecret(idpSigningCertificateSegments[2].Trim('/'), idpSigningCertificateSegments[3].TrimEnd('/')).Value.Value);
                builder.AddSigningCredential(new X509Certificate2(idpSigningCertificateBytes), "RS256");
            }
            catch
            {
                builder.AddDeveloperSigningCredential();
            }

            builder.AddMutualTlsSecretValidators();

            services.AddScoped<IViewRender, ViewRender>();
            services.Configure<SmsOptions>(Configuration.GetSection("SMSSettings"));
            services.Configure<EmailOptions>(Configuration.GetSection("EmailSettings"));
            services.Configure<GoogleCaptchaOptions>(Configuration.GetSection("GoogleCaptcha"));

            services.AddSingleton<ISmsSender, MessageSender>();
            services.AddSingleton<IEmailSender, MessageSender>();
            services.AddSingleton<IEventSink, IdentityServerEventSink>();

            services.AddTransient<IEventSink, IdentityServerStatsSink>();
            services.AddTransient<IActionContextAccessor, ActionContextAccessor>();
            services.AddTransient<IEmailService, EmailController>();
            services.AddTransient<IPasswordTopologyProvider, PasswordTopologyProvider>();

            services.PostConfigure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme, option =>
            {
                //option.Cookie.Name = "Hello";
            });

            Activity.DefaultIdFormat = ActivityIdFormat.W3C;
            Activity.ForceDefaultIdFormat = true;

            services.AddOpenTelemetryTracing(
                (builder) => builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault().AddService("beyondauth-authentication"))
                    .AddSource(nameof(IdentityServerEventSink))
                    //.AddProcessor(new OpenTelemetryFilteredProcessor(new BatchActivityExportProcessor(new OpenTelemetryRavenDbExporter()), (act) => true)) //TODO: add filter
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    //.AddOtlpExporter(opt => opt.Endpoint = new Uri("grafana-agent:55680"))
                    .AddConsoleExporter()
                    );

            services.AddOpenTelemetryMetrics(builder =>
            {
                builder.AddAspNetCoreInstrumentation();
                builder.AddHttpClientInstrumentation();
                builder.AddPrometheusExporter(opt => opt.ScrapeResponseCacheDurationMilliseconds = 15000);
            });
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
                app.UseExceptionHandler(new ExceptionHandlerOptions { ExceptionHandlingPath = "/home/error", AllowStatusCode404Response = false });
            }

            app.UseForwardedHeaders();
            app.UseHsts();
            app.UseHttpsRedirection();
            app.UseCertificateForwarding();

            app.UseResponseCaching();

            var jsnlogConfiguration = new JsnlogConfiguration();
            app.UseJSNLog(new LoggingAdapter(loggerFactory), jsnlogConfiguration);

            app.UseStaticFiles();

            //app.UseCors(x => x.AllowAnyOrigin().WithHeaders("accept", "authorization", "content-type", "origin").AllowAnyMethod());

            var policyCollection = new HeaderPolicyCollection()
                .AddFrameOptionsSameOrigin()
                .AddXssProtectionBlock()
                .AddContentTypeOptionsNoSniff()
                .AddStrictTransportSecurityMaxAgeIncludeSubDomainsAndPreload(maxAgeInSeconds: 60 * 60 * 24 * 365) // maxage = one year in seconds
                .AddReferrerPolicyStrictOriginWhenCrossOrigin()
                .RemoveServerHeader()
                .AddContentSecurityPolicy(builder =>
                {
                    builder.AddDefaultSrc().Self();
                    builder.AddObjectSrc().None();
                    builder.AddFrameAncestors().None();
                    builder.AddFormAction().Self();
                    builder.AddImgSrc().Self().Data().From("opsai.blob.core.windows.net");
                    builder.AddScriptSrc().Self().From("cdnjs.cloudflare.com").From("ajax.cloudflare.com").From("static.cloudflareinsights.com");
                    builder.AddStyleSrc().Self().UnsafeInline();

                    //default-src 'self'; object-src 'none'; frame-ancestors 'none'; sandbox allow-forms allow-same-origin allow-scripts; base-uri 'self';
                })
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

            app.UseRouting();
            app.UseMultiTenant();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseIdentityServer();

            app.UseSecurityHeaders(policyCollection);
            app.UseHealthChecks(Configuration["HealthChecks:FullEndpoint"], new HealthCheckOptions()
            {
                Predicate = _ => true,
                ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
            });

            app.UseHealthChecks(Configuration["HealthChecks:SummaryEndpoint"], new HealthCheckOptions()
            {
                Predicate = _ => _.FailureStatus == HealthStatus.Unhealthy
            });

            app.UseCookiePolicy(new CookiePolicyOptions
            {
                Secure = CookieSecurePolicy.Always,
                MinimumSameSitePolicy = SameSiteMode.None
            });

            app.UseCorrelationId();
            app.UseOpenTelemetryPrometheusScrapingEndpoint();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
                endpoints.MapFallbackToController("PageNotFound", "Home");
            });
        }

        private async Task HandleOnRemoteFailure(RemoteFailureContext context)
        {
            context.Response.StatusCode = 500;
            context.Response.ContentType = "text/html";
            await context.Response.WriteAsync("<html><body>");
            await context.Response.WriteAsync("A remote failure has occurred: <br>" +
                context.Failure.Message.Split(Environment.NewLine).Select(s => HtmlEncoder.Default.Encode(s) + "<br>").Aggregate((s1, s2) => s1 + s2));

            if (context.Properties != null)
            {
                await context.Response.WriteAsync("Properties:<br>");
                foreach (var pair in context.Properties.Items)
                {
                    await context.Response.WriteAsync($"-{ HtmlEncoder.Default.Encode(pair.Key)}={ HtmlEncoder.Default.Encode(pair.Value)}<br>");
                }
            }

            await context.Response.WriteAsync("<a href=\"/\">Home</a>");
            await context.Response.WriteAsync("</body></html>");

            // context.Response.Redirect("/error?FailureMessage=" + UrlEncoder.Default.Encode(context.Failure.Message));

            context.HandleResponse();
        }

    }
}
