using Authentication.Extensions;
using Authentication.Models.Account;
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
using Authentication.Models;
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

            var dataProtection = services.AddDataProtection().SetApplicationName("auth.ops.ai");

            if (!string.IsNullOrEmpty(Configuration["DataProtection:KeyIdentifier"]))
                dataProtection
                    .ProtectKeysWithAzureKeyVault(Configuration["DataProtection:KeyIdentifier"], Configuration["DataProtection:ClientId"], Configuration["DataProtection:ClientSecret"])
                    .PersistKeysToAzureBlobStorage(new Uri(Configuration["DataProtection:StorageUri"]));

            services.AddMultiTenant<TenantSetting>().WithHostStrategy("__tenant__").WithStore(new ServiceLifetime(), (sp) => new RavenDBMultitenantStore(sp.GetService<IDocumentStore>(), sp.GetService<IMemoryCache>()))
                .WithPerTenantOptions<AccountOptions>((options, tenantInfo) =>
                {
                    options.AllowLocalLogin = tenantInfo.AccountOptions.AllowLocalLogin;
                    options.AllowRememberLogin = tenantInfo.AccountOptions.AllowRememberLogin;
                    options.AutomaticRedirectAfterSignOut = tenantInfo.AccountOptions.AutomaticRedirectAfterSignOut;
                    options.IncludeWindowsGroups = tenantInfo.AccountOptions.IncludeWindowsGroups;
                    options.InvalidCredentialsErrorMessage = tenantInfo.AccountOptions.InvalidCredentialsErrorMessage;
                    options.RememberMeLoginDuration = tenantInfo.AccountOptions.RememberMeLoginDuration;
                    options.ShowLogoutPrompt = tenantInfo.AccountOptions.ShowLogoutPrompt;
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
                .WithPerTenantOptions<UserStoreOptions>((options, tenantInfo) =>
                {
                    options.DatabaseName = $"TenantIdentity-{tenantInfo.Identifier}";
                })
                .WithPerTenantOptions<CookieAuthenticationOptions>((o, tenantInfo) =>
                {
                    o.Cookie.Name += tenantInfo.Id;
                })
                .WithPerTenantOptions<Microsoft.AspNetCore.Authentication.Google.GoogleOptions>((o, tenantInfo) =>
                {
                    o.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;

                    // register your IdentityServer with Google at https://console.developers.google.com
                    // enable the Google+ API
                    // set the redirect URI to http://localhost:5000/signin-google

                    //o.ClientId = Configuration["ExternalIdps:Google:ClientId"];
                    //o.ClientSecret = Configuration["ExternalIdps:Google:ClientSecret"];
                });

            services.AddControllersWithViews();
            var razorBuilder = services.AddRazorPages();
#if DEBUG
            razorBuilder.AddRazorRuntimeCompilation();
#endif
            services.AddSameSiteCookiePolicy();

            services.AddSingleton((ctx) =>
            {
                IDocumentStore store = new DocumentStore
                {
                    Urls = new[] { Configuration["Raven:Url"] },
                    Database = Configuration["Raven:Database"],
                    Certificate = Configuration.GetSection("Raven:EncryptionEnabled").Get<bool>() ? new X509Certificate2(Configuration["Raven:CertFile"], Configuration["Raven:CertPassword"]) : null
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

            var identityBuilder = services.AddIdentity<ApplicationUser, Raven.Identity.IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddDefaultTokenProviders();

            identityBuilder.Services.AddScoped<IUserStore<ApplicationUser>, UserStore<ApplicationUser, Raven.Identity.IdentityRole>>();
            identityBuilder.Services.AddScoped<IRoleStore<Raven.Identity.IdentityRole>, RoleStore<Raven.Identity.IdentityRole>>();

            var healthChecks = services.AddHealthChecks()
                .AddRavenDB(setup => { setup.Urls = new[] { Configuration["Raven:Url"] }; setup.Database = Configuration["Raven:Database"]; setup.Certificate = Configuration.GetSection("Raven:EncryptionEnabled").Get<bool>() ? new X509Certificate2(Configuration["Raven:CertFile"], Configuration["Raven:CertPassword"]) : null; }, "ravendb")
                .AddIdentityServer(new Uri(Configuration["BaseUrl"]), "openid-connect");

            if (!string.IsNullOrEmpty(Configuration["DataProtection:KeyIdentifier"]))
                healthChecks.AddAzureKeyVault(new Uri(Configuration["DataProtection:VaultUrl"]), new DefaultAzureCredential(), options =>
                {
                    options.UseClientSecrets(Configuration["DataProtection:ClientId"], Configuration["DataProtection:ClientSecret"]);
                    options.UseKeyVaultUrl(Configuration["DataProtection:VaultUrl"]);
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
            //services.AddTransient<ICorsPolicyService, CorsPolicyService>();

            var builder = services.AddIdentityServer(options =>
            {
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;

                options.MutualTls.Enabled = true;
                options.MutualTls.ClientCertificateAuthenticationScheme = "Certificate";
            })
                //.AddSigningCredential()
                .AddPersistedGrantStore<RavenDBPersistedGrantStore>()
                .AddClientStore<RavenDBClientStore>()
                .AddResourceStore<RavenDBResourceStore>()
                .AddCorsPolicyService<CorsPolicyService>()
                .AddAspNetIdentity<ApplicationUser>()
                .AddResourceOwnerValidator<ResourceOwnerPasswordValidator<ApplicationUser>>()
                .AddProfileService<ProfileService<ApplicationUser>>()
                ;

            builder.AddMutualTlsSecretValidators();
            builder.AddDeveloperSigningCredential();

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCertificate("Certificate", options =>
                {
                    // allows both self-signed and CA-based certs. Check the MTLS spec for details.
                    options.AllowedCertificateTypes = CertificateTypes.All;
                }).AddCookie(options =>
                {
                    options.Cookie.Name = "BA.";
                })

            .AddGoogle();

            services.AddScoped<IViewRender, ViewRender>();
            services.Configure<SmsOptions>(Configuration.GetSection("SMSSettings"));
            services.Configure<EmailOptions>(Configuration.GetSection("EmailSettings"));
            services.Configure<GoogleCaptchaOptions>(Configuration.GetSection("GoogleCaptcha"));

            services.AddSingleton<ISmsSender, MessageSender>();
            services.AddSingleton<IEmailSender, MessageSender>();
            services.AddTransient<IActionContextAccessor, ActionContextAccessor>();
            services.AddTransient<IEmailService, EmailController>();

            services.PostConfigure<CookieAuthenticationOptions>(IdentityConstants.ApplicationScheme, option =>
            {
                //option.Cookie.Name = "Hello";
            });

            services.AddApplicationInsightsTelemetry(Configuration["APPINSIGHTS_CONNECTIONSTRING"]);
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
                var options = new RewriteOptions()
                    .AddRedirectToHttpsPermanent()
                    .AddRedirectToNonWwwPermanent();
                app.UseRewriter(options);

                app.UseHttpsRedirection();
            }

            app.UseHttpsRedirection();

            app.UseResponseCaching();

            var jsnlogConfiguration = new JsnlogConfiguration();
            app.UseJSNLog(new LoggingAdapter(loggerFactory), jsnlogConfiguration);

            app.UseStaticFiles();

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

            app.Use(async (context, next) =>
            {
                context.Response.Headers.Add("Feature-Policy", "geolocation 'none';midi 'none';notifications 'none';push 'none';sync-xhr 'none';microphone 'none';camera 'none';magnetometer 'none';gyroscope 'none';speaker 'self';vibrate 'none';fullscreen 'self';payment 'none';");
                await next.Invoke();
            });

            app.UseRouting();
            app.UseAuthentication();
            app.UseIdentityServer();

            app.UseAuthorization();

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

            app.UseMultiTenant();

            app.UseCorrelationId();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
                endpoints.MapFallbackToController("PageNotFound", "Home");
            });
        }
    }
}
