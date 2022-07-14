using AspNet.Security.OAuth.GitHub;
using Audit.Core;
using Audit.NET.RavenDB.ConfigurationApi;
using Authentication.Controllers;
using Authentication.Domain;
using Authentication.Extensions;
using Authentication.Infrastructure;
using Authentication.Services;
using Autofac;
using Azure.Core;
using Azure.Identity;
using Azure.Security.KeyVault.Certificates;
using Azure.Security.KeyVault.Keys;
using Azure.Security.KeyVault.Secrets;
using Azure.Storage.Blobs;
using BeyondAuth.PasswordValidators.Common;
using BeyondAuth.PasswordValidators.Topology;
using BlackstarSolar.AspNetCore.Identity.PwnedPasswords;
using CorrelationId;
using CorrelationId.DependencyInjection;
using HealthChecks.UI.Client;
using Identity.Core;
using Identity.Core.Settings;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.AspNetIdentity;
using IdentityServer4.Contrib.RavenDB.Options;
using IdentityServer4.Contrib.RavenDB.Services;
using IdentityServer4.Contrib.RavenDB.Stores;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores.Serialization;
using JSNLog;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Certificate;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Facebook;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authentication.Twitter;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.FeatureManagement;
using Microsoft.FeatureManagement.FeatureFilters;
using Microsoft.IdentityModel.Logging;
using Newtonsoft.Json;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Prometheus;
using Prometheus.SystemMetrics;
using Prometheus.SystemMetrics.Collectors;
using Raven.Client.Documents;
using Raven.Client.Documents.Conventions;
using Raven.Client.Documents.Linq;
using Raven.Client.Documents.Session;
using Raven.Client.Json.Serialization.NewtonsoftJson;
using Raven.DependencyInjection;
using Raven.Identity;
using SimpleHelpers;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.Encodings.Web;
using Toggly.FeatureManagement;
using Toggly.FeatureManagement.Helpers;
using Toggly.FeatureManagement.Storage.RavenDB;
using Toggly.FeatureManagement.Web;
using Toggly.FeatureManagement.Web.Configuration;
using Toggly.FeatureManagement.Storage.RavenDB.Configuration;
using System.IdentityModel.Tokens.Jwt;

namespace Authentication
{
    public class Startup
    {
        public Startup(IConfiguration configuration) => Configuration = configuration;

        public IConfiguration Configuration { get; }

        public ILifetimeScope AutofacContainer { get; private set; }

        X509Certificate2 ravenDBcert = null;

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

            services.AddHttpClient();
            services.AddTogglyWeb(options =>
            {
                options.AppKey = Configuration["Toggly:AppKey"];
                options.Environment = Configuration["Toggly:Environment"];
            });
            services.AddTogglyRavenDbSnapshotProvider();

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

            if (!string.IsNullOrEmpty(Environment.GetEnvironmentVariable("VaultUri")))
            {
                TokenCredential? clientCredential = Environment.GetEnvironmentVariable("ClientId") != null ? new ClientSecretCredential(Environment.GetEnvironmentVariable("TenantId"), Environment.GetEnvironmentVariable("ClientId"), Environment.GetEnvironmentVariable("ClientSecret")) : null;
                var keyClient = new KeyClient(new Uri(Environment.GetEnvironmentVariable("VaultUri")!), clientCredential ?? new DefaultAzureCredential());
                var blobClient = new BlobClient(Configuration["DataProtection:StorageConnectionString"], Configuration["DataProtection:StorageContainer"], "keys.xml");

                dataProtection
                    .ProtectKeysWithAzureKeyVault(keyClient.GetKey("DataProtection").Value.Id, clientCredential ?? new DefaultAzureCredential())
                    .PersistKeysToAzureBlobStorage(new Uri(Configuration["DataProtection:StorageUri"]));
            }

            services.AddAuthorization();

            services.AddDistributedMemoryCache();
            services.AddOidcStateDataFormatterCache();

            //JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Clear();

            var authenticationServices = services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme);
            authenticationServices.AddCertificate("Certificate", options =>
            {
                // allows both self-signed and CA-based certs. Check the MTLS spec for details.
                options.AllowedCertificateTypes = CertificateTypes.All;
            })
                .AddCookie(options => options.Cookie.Name = "BA.Auth")
                .AddOpenIdConnect(options => { options.ClientId = "__tenant__"; options.ClientSecret = "__tenant__"; options.Authority = "__tenant__"; options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme; })
                .AddGoogle(options => { options.ClientId = "__tenant__"; options.ClientSecret = "__tenant__"; options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme; })
                .AddFacebook(options => { options.ClientId = "__tenant__"; options.ClientSecret = "__tenant__"; options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme; })
                .AddTwitter(options => { options.ConsumerKey = "__tenant__"; options.ConsumerSecret = "__tenant__"; options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme; })
                .AddMicrosoftAccount(options => { options.ClientId = "__tenant__"; options.ClientSecret = "__tenant__"; options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme; })
                .AddGitHub(options => { options.ClientId = "__tenant__";  options.ClientSecret = "__tenant__"; options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme; });

            var identityBuilder = services.AddIdentity<ApplicationUser, Raven.Identity.IdentityRole>(options => { })
                .AddDefaultTokenProviders()
                .AddPasswordValidator<EmailAsPasswordValidator<ApplicationUser>>()
                .AddPasswordValidator<InvalidPhrasePasswordValidator<ApplicationUser>>()
                .AddPwnedPasswordsValidator<ApplicationUser>(options => options.ApiKey = Configuration["HaveIBeenPwned:ApiKey"])
                .AddTop1000PasswordValidator<ApplicationUser>()
                .AddPasswordValidator<PasswordTopologyValidator<ApplicationUser>>();

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
                .AddProfileService<Extensions.ProfileService<ApplicationUser>>()
                .AddUserSession<RavenDbSessionProvider>();

            #region Multitenant

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
                    options.SignupUrl = tenantInfo.AccountOptions.SignupUrl;
                    options.SignupMessage = tenantInfo.AccountOptions.SignupMessage;
                    options.SignupText = tenantInfo.AccountOptions.SignupText;
                    options.EnableLockedOutMessage = tenantInfo.AccountOptions.EnableLockedOutMessage;
                    options.LockedOutErrorMessage = tenantInfo.AccountOptions.LockedOutErrorMessage;
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
                    //options.AddScheme<GoogleHandler>(GoogleDefaults.AuthenticationScheme, "Google");
                    //options.AddScheme(GoogleDefaults.AuthenticationScheme, config =>
                    //{
                    //    config.HandlerType = typeof(GoogleHandler);
                    //    config.DisplayName = "Google";
                    //});
                    //options.AddScheme(GitHubAuthenticationDefaults.AuthenticationScheme, config =>
                    //{
                    //    config.HandlerType = typeof(GitHubAuthenticationHandler);
                    //    config.DisplayName = GitHubAuthenticationDefaults.DisplayName;
                    //});
                    //options.RequireAuthenticatedSignIn
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
                    if (tenantInfo?.IdentityOptions != null)
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
                    }
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
                    o.Events = new CookieAuthenticationEvents
                    {
                        OnValidatePrincipal = async ctx =>
                        {
                            if (ctx.Scheme.Name == "Identity.Application")
                            {
                                var store = ctx.HttpContext.RequestServices.GetRequiredService<IDocumentStore>();
                                using (var session = store.OpenAsyncSession($"TenantIdentity-{tenantInfo.Identifier}"))
                                {
                                    if (!await session.Advanced.ExistsAsync($"UserSessions/{ctx.Properties.GetString("session_id")}").ConfigureAwait(false)) //TODO:  || userSession.UserAgent != ctx.Request.Headers.UserAgent Replace with a smart parser taking into account browser upgrades
                                    {
                                        await ctx.HttpContext.SignOutAsync();
                                        ctx.RejectPrincipal();
                                    }
                                    else
                                    {
                                        session.Advanced.Patch<UserSession, DateTime>($"UserSessions/{ctx.Properties.GetString("session_id")}", t => t.LastSeenOnUtc, DateTime.UtcNow);
                                        //if (!userSession.IPAddresses.Contains(ctx.Request.HttpContext.Connection.RemoteIpAddress.ToString()))
                                        //    session.Advanced.Patch<UserSession, string>($"UserSessions/{ctx.Properties.GetString("session_id")}", t => t.IPAddresses, t => t.Add(ctx.Request.HttpContext.Connection.RemoteIpAddress.ToString()));
                                        await session.SaveChangesAsync();
                                    }
                                }
                            }
                        },
                        OnSignedIn = async ctx =>
                        {
                            if (ctx.Scheme.Name == "Identity.Application")
                            {
                                var store = ctx.HttpContext.RequestServices.GetRequiredService<IDocumentStore>();
                                using (var session = store.OpenAsyncSession($"TenantIdentity-{tenantInfo.Identifier}"))
                                {
                                    var userSessions = new UserSession
                                    {
                                        Id = $"UserSessions/{ctx.Properties.GetString("session_id")}",
                                        BrowserIds = new List<string> { ctx.Properties.GetString("browser_id") },
                                        UserId = ctx.Principal.FindFirstValue(JwtClaimTypes.Subject),
                                        IPAddresses = new List<string> { ctx.Request.HttpContext.Connection.RemoteIpAddress.ToString() },
                                        UserAgent = ctx.Request.Headers.UserAgent.ToString(),
                                        Idp = ctx.Principal.FindFirstValue(JwtClaimTypes.IdentityProvider),
                                        Amr = ctx.Principal.FindFirstValue(JwtClaimTypes.AuthenticationMethod),
                                        MaxExpireOnUtc = ctx.Properties.ExpiresUtc
                                    };
                                    await session.StoreAsync(userSessions);
                                    session.Advanced.GetMetadataFor(userSessions)["@expires"] = ctx.Properties.ExpiresUtc ?? DateTime.UtcNow.AddYears(1);
                                    if (ctx.Properties.GetString("browser_id") != null)
                                    {
                                        var browserInfo = await session.LoadAsync<UserBrowser>($"UserBrowsers/{ctx.Properties.GetString("session_id")}");
                                        if (browserInfo == null)
                                        {
                                            browserInfo = new UserBrowser { Id = $"UserBrowsers/{ctx.Properties.GetString("browser_id")}", UserAgent = ctx.Request.Headers.UserAgent.ToString(), IPAddresses = new List<string> { ctx.Request.HttpContext.Connection.RemoteIpAddress.ToString() } };
                                            await session.StoreAsync(browserInfo);
                                        }
                                        if (!browserInfo.UserIds.ContainsKey(ctx.Principal.FindFirstValue("sub")))
                                            browserInfo.UserIds.Add(ctx.Principal.FindFirstValue("sub"), DateTime.UtcNow);
                                        else
                                            browserInfo.UserIds[ctx.Principal.FindFirstValue("sub")] = DateTime.UtcNow;
                                        if (!browserInfo.IPAddresses.Contains(ctx.Request.HttpContext.Connection.RemoteIpAddress.ToString()))
                                            browserInfo.IPAddresses.Add(ctx.Request.HttpContext.Connection.RemoteIpAddress.ToString());
                                        browserInfo.LastSeenOnUtc = DateTime.UtcNow;
                                    }
                                    await session.SaveChangesAsync();
                                }
                            }
                        },
                        OnSigningOut = async ctx =>
                        {
                            if (ctx.Scheme.Name == "Identity.Application")
                            {
                                var store = ctx.HttpContext.RequestServices.GetRequiredService<IDocumentStore>();
                                using (var session = store.OpenAsyncSession($"TenantIdentity-{tenantInfo.Identifier}"))
                                {
                                    var userSession = await session.LoadAsync<UserSession>($"UserSessions/{ctx.Request.Cookies["idsrv.session"]}");
                                    if (userSession.UserId == ctx.HttpContext.User.FindFirstValue("sub"))
                                    {
                                        session.Delete(userSession);
                                        await session.SaveChangesAsync().ConfigureAwait(false);
                                    }
                                }
                            }
                        }
                    };
                })
                .WithPerTenantOptions<GoogleOptions>((o, tenantInfo) =>
                {
                    if (!tenantInfo.ExternalIdps.Any(t => t.Name.Equals("Google") && t.Enabled))
                        return;

                    // register your IdentityServer with Google at https://console.developers.google.com
                    // enable the Google+ API
                    // set the redirect URI to https://localhost:5001/signin-google

                    var googleSettings = tenantInfo.ExternalIdps.First(t => t.Name == "Google") as ExternalOidcIdentityProvider;

                    o.ClientId = googleSettings.ClientId;
                    o.ClientSecret = googleSettings.ClientSecret;

                    o.Scope.Add("user");
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
                        return;

                    var openIdConnectSettings = tenantInfo.ExternalIdps.First(t => !t.Name.In("MicrosoftAccount", "Twitter", "Facebook", "Google")) as ExternalOidcIdentityProvider;

                    o.Configuration = new Microsoft.IdentityModel.Protocols.OpenIdConnect.OpenIdConnectConfiguration
                    {
                        AuthorizationEndpoint = "https://github.com/login/oauth/authorize",
                        TokenEndpoint = "https://github.com/login/oauth/access_token",
                        UserInfoEndpoint = "https://api.github.com/user"

                    };
                    //o.Authority = openIdConnectSettings.Authority;
                    o.ClientId = openIdConnectSettings.ClientId;
                    o.ClientSecret = openIdConnectSettings.ClientSecret;
                    o.ResponseType = "code";
                    o.SaveTokens = true;
                    o.ResponseMode = "query";
                    o.Scope.Add("offline_access");
                    o.Events = new OpenIdConnectEvents()
                    {
                        OnRemoteFailure = HandleOnRemoteFailure
                    };
                })
                .WithPerTenantOptions<GitHubAuthenticationOptions>((o, tenantInfo) =>
                {
                    if (!tenantInfo.ExternalIdps.Any(t => t.Name.In("GitHub") && t.Enabled))
                        return;

                    var openIdConnectSettings = tenantInfo.ExternalIdps.First(t => t.Name.In("GitHub")) as ExternalOidcIdentityProvider;

                    o.ClaimActions.MapJsonKey(JwtClaimTypes.Name, "name");
                    o.ClaimActions.MapJsonKey("github_url", "html_url");
                    o.ClaimActions.MapJsonKey(JwtClaimTypes.Email, "email");
                    o.ClaimActions.MapJsonKey("organization", "company");
                    o.ClaimActions.MapJsonKey("two_factor_authentication", "two_factor_authentication");
                    o.ClaimActions.Remove(ClaimTypes.Name);
                    o.ClaimActions.Remove(ClaimTypes.Email);
                    o.ClaimActions.Remove("urn:github:name");
                    o.ClaimActions.Remove("urn:github:url");

                    o.ClientId = openIdConnectSettings.ClientId;
                    o.ClientSecret = openIdConnectSettings.ClientSecret;
                    o.SaveTokens = true;
                    o.Scope.Add("user");
                    o.Events = new OAuthEvents
                    {
                        OnRemoteFailure = HandleOnRemoteFailure
                    };
                })
                .WithPerTenantAuthentication();
            #endregion
            
            services.AddControllersWithViews();
            var razorBuilder = services.AddRazorPages();
#if DEBUG
            razorBuilder.AddRazorRuntimeCompilation();
#endif
            services.AddSameSiteCookiePolicy();

            if (Environment.GetEnvironmentVariable("VaultUri") != null)
            {
                TokenCredential? clientCredential = Environment.GetEnvironmentVariable("ClientId") != null ? new ClientSecretCredential(Environment.GetEnvironmentVariable("TenantId"), Environment.GetEnvironmentVariable("ClientId"), Environment.GetEnvironmentVariable("ClientSecret")) : null;

                var certificateClient = new CertificateClient(vaultUri: new Uri(Environment.GetEnvironmentVariable("VaultUri")), credential: clientCredential ?? new DefaultAzureCredential());
                var secretClient = new SecretClient(new Uri(Environment.GetEnvironmentVariable("VaultUri")), clientCredential ?? new DefaultAzureCredential());

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

            identityBuilder.Services.AddScoped<IUserStore<ApplicationUser>, UserStore<ApplicationUser, Raven.Identity.IdentityRole>>();
            identityBuilder.Services.AddScoped<IRoleStore<Raven.Identity.IdentityRole>, RoleStore<Raven.Identity.IdentityRole>>();

            var healthChecks = services.AddHealthChecks()
                .AddRavenDB(setup => { setup.Urls = Configuration.GetSection("Raven:Urls").Get<string[]>(); setup.Database = Configuration["Raven:Database"]; setup.Certificate = ravenDBcert; }, "ravendb", timeout: new TimeSpan(0, 0, 2))
                /*.AddIdentityServer(new Uri(Configuration["BaseUrl"]), "openid-connect", HealthStatus.Degraded)*/;

            if (Environment.GetEnvironmentVariable("VaultUri") != null)
            {
                TokenCredential? clientCredential = Environment.GetEnvironmentVariable("ClientId") != null ? new ClientSecretCredential(Environment.GetEnvironmentVariable("TenantId"), Environment.GetEnvironmentVariable("ClientId"), Environment.GetEnvironmentVariable("ClientSecret")) : null;

                healthChecks.AddAzureKeyVault(new Uri(Environment.GetEnvironmentVariable("VaultUri")), clientCredential ?? new DefaultAzureCredential(), options =>
                {

                }, "vault", HealthStatus.Degraded, timeout: new TimeSpan(0, 0, 2));
            }

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

            try
            {
                TokenCredential? clientCredential = Environment.GetEnvironmentVariable("ClientId") != null ? new ClientSecretCredential(Environment.GetEnvironmentVariable("TenantId"), Environment.GetEnvironmentVariable("ClientId"), Environment.GetEnvironmentVariable("ClientSecret")) : null;

                var certificateClient = new CertificateClient(vaultUri: new Uri(Environment.GetEnvironmentVariable("VaultUri")), credential: clientCredential ?? new DefaultAzureCredential());
                var secretClient = new SecretClient(new Uri(Environment.GetEnvironmentVariable("VaultUri")), clientCredential ?? new DefaultAzureCredential());

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
            services.AddSingleton<IEmailSender, MailgunMessageSender>();
            services.AddSingleton<IEventSink, IdentityServerEventSink>();
            services.AddSingleton<IEventSink, IdentityServerStatsSink>();
            services.AddSingleton<IEventSink, IdentityServerAuditSink>();
            services.AddTransient<IActionContextAccessor, ActionContextAccessor>();
            services.AddTransient<IPasswordTopologyProvider, PasswordTopologyProvider>();
            services.AddTransient<IOtacManager, OtacManager>();

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
            });

            services.AddSystemMetrics(registerDefaultCollectors: false);
            services.AddSystemMetricCollector<WindowsMemoryCollector>();
            services.AddSystemMetricCollector<LoadAverageCollector>();

            services.AddPrometheusCounters();
            services.AddPrometheusAspNetCoreMetrics();
            services.AddPrometheusHttpClientMetrics();

            if (Configuration.GetValue<bool>("FeatureManagement:PasswordResetService"))
                services.AddHostedService<PasswordResetService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            IdentityModelEventSource.ShowPII = true;
            app.UseExceptionHandler(new ExceptionHandlerOptions { ExceptionHandlingPath = "/error", AllowStatusCode404Response = false });

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

            app.UseHsts();
            //app.UseHttpsRedirection();
            app.UseCertificateForwarding();

            var jsnlogConfiguration = new JsnlogConfiguration();
            app.UseJSNLog(new LoggingAdapter(loggerFactory), jsnlogConfiguration);

            app.UseStaticFiles();
            
            Audit.Core.Configuration.Setup()
                .JsonNewtonsoftAdapter(new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore })
                .UseRavenDB(config => config
                .WithSettings(settings => settings
                    .Urls(Configuration.GetSection("Raven:Urls").Get<string[]>())
                    .Database(ev => app.ApplicationServices.GetService<IOptions<IdentityStoreOptions>>().Value.DatabaseName)
                    .Certificate(ravenDBcert)));

            Audit.Core.Configuration.AddCustomAction(ActionType.OnEventSaving, scope =>
            {
                var httpContextAccessor = app.ApplicationServices.GetService<IHttpContextAccessor>();
                if (httpContextAccessor?.HttpContext?.User?.FindFirstValue("sub") != null)
                    scope.SetCustomField("UserId", httpContextAccessor?.HttpContext?.User?.FindFirstValue("sub"));
                if (httpContextAccessor?.HttpContext != null)
                    scope.SetCustomField("UserAgent", httpContextAccessor.HttpContext.Request.Headers.UserAgent.ToString());
                if (httpContextAccessor?.HttpContext?.User?.FindFirstValue("sub") != null)
                    scope.SetCustomField("BrowserId", httpContextAccessor?.HttpContext?.User?.FindFirstValue("browser_id"));

                var auditEvent = scope.Event;
                if (auditEvent.Target != null)
                {
                    var diff = ObjectDiffPatch.GenerateDiff(auditEvent.Target.Old, auditEvent.Target.New);
                    auditEvent.Target.Old = diff.OldValues;
                    auditEvent.Target.New = diff.NewValues;
                }

                if (!auditEvent.CustomFields.ContainsKey("RemoteIpAddress") && httpContextAccessor?.HttpContext?.Connection.RemoteIpAddress != null)
                    scope.SetCustomField("RemoteIpAddress", httpContextAccessor.HttpContext.Connection.RemoteIpAddress.ToString());
            });

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
                    builder.AddScriptSrc().Self().From("cdnjs.cloudflare.com").From("ajax.cloudflare.com").From("static.cloudflareinsights.com").UnsafeEval();
                    builder.AddStyleSrc().Self().UnsafeInline();

                    //default-src 'self'; object-src 'none'; frame-ancestors 'none'; sandbox allow-forms allow-same-origin allow-scripts; base-uri 'self';
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

            app.UseCookiePolicy(new CookiePolicyOptions
            {
                Secure = CookieSecurePolicy.Always,
                MinimumSameSitePolicy = SameSiteMode.None
            });

            app.UseRouting();
            app.UseMultiTenant();
            app.UseAuthentication();
            app.UseAuthorization();
            app.UseIdentityServer();

            app.UseRequestLocalization();

            //app.UseCors(x => x.AllowAnyOrigin().WithHeaders("accept", "authorization", "content-type", "origin").AllowAnyMethod());
            app.UseResponseCaching();
            app.UseResponseCompression();

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

            app.UseCorrelationId();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapMetrics();
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
                    await context.Response.WriteAsync($"-{HtmlEncoder.Default.Encode(pair.Key)}={HtmlEncoder.Default.Encode(pair.Value)}<br>");
                }
            }

            await context.Response.WriteAsync("<a href=\"/\">Home</a>");
            await context.Response.WriteAsync("</body></html>");

            context.HandleResponse();
        }

    }
}
