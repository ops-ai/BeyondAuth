using Authentication.Extensions;
using Authentication.Models.Account;
using Authentication.Options;
using Autofac;
using Azure.Core;
using Azure.Identity;
using CorrelationId.DependencyInjection;
using HealthChecks.UI.Client;
using Identity.Core;
using IdentityServer.LdapExtension.Extensions;
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
using Microsoft.Rest;
using Newtonsoft.Json;
using Raven.Client.Documents;
using System;
using System.Linq;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using Raven.Client.Json.Serialization;
using Raven.Client.Json.Serialization.NewtonsoftJson;
using Authentication.Models;
using Authentication.Stores;
using IdentityServer.LdapExtension;
using IdentityServer.LdapExtension.UserStore;
using IdentityServer4;

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
            services.Configure<ExtensionConfig>(Configuration.GetSection("IdentityServerLdap"));

            services.AddCorrelationId();

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

            services.AddIdentity<ApplicationUser, Raven.Identity.IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true).AddDefaultTokenProviders();

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
            services.AddRazorPages();
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

            var healthChecks = services.AddHealthChecks()
                .AddRavenDB(setup => { setup.Urls = new[] { Configuration["Raven:Url"] }; setup.Database = Configuration["Raven:Database"]; setup.Certificate = Configuration.GetSection("Raven:EncryptionEnabled").Get<bool>() ? new X509Certificate2(Configuration["Raven:CertFile"], Configuration["Raven:CertPassword"]) : null; }, "ravendb")
                .AddIdentityServer(new Uri(Configuration["BaseUrl"]), "openid-connect");

            if (!string.IsNullOrEmpty(Configuration["DataProtection:KeyIdentifier"]))
                healthChecks.AddAzureKeyVault(new Uri(Configuration["DataProtection:VaultUrl"]), new DefaultAzureCredential(), options =>
                {
                    options.UseClientSecrets(Configuration["DataProtection:ClientId"], Configuration["DataProtection:ClientSecret"]);
                    options.UseKeyVaultUrl(Configuration["DataProtection:VaultUrl"]);
                });

            //services.AddTransient<IRedirectUriValidator, DemoRedirectValidator>();
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
                /*.AddLdapUsers<ApplicationUser, RavenDBUserStore<ApplicationUser>>(Configuration.GetSection("IdentityServerLdap"))*/;

            builder.Services.AddSingleton<ILdapService<ApplicationUser>, LdapService<ApplicationUser>>();

            // For testing purpose we can use the in memory. In reality it's better to have
            // your own implementation. An example with Redis exists in the repository
            builder.Services.AddSingleton(typeof(RavenDBUserStore<ApplicationUser>));
            builder.Services.AddSingleton(serviceProvider => (ILdapUserStore)serviceProvider.GetService(typeof(RavenDBUserStore<ApplicationUser>)));
            builder.AddProfileService<LdapUserProfileService<ApplicationUser>>();
            builder.AddResourceOwnerValidator<LdapUserResourceOwnerPasswordValidator<ApplicationUser>>();


            builder.AddMutualTlsSecretValidators();
            builder.AddDeveloperSigningCredential();

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCertificate("Certificate", options =>
                {
                    // allows both self-signed and CA-based certs. Check the MTLS spec for details.
                    options.AllowedCertificateTypes = CertificateTypes.All;
                }).AddCookie(options =>
                {
                    options.Cookie.Name = "MyAppCookie.";
                })

            .AddGoogle();
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
                app.UseExceptionHandler("/Home/Error");
            }
            app.UseCookiePolicy();
            app.UseHttpsRedirection();

            var jsnlogConfiguration = new JsnlogConfiguration();
            app.UseJSNLog(new LoggingAdapter(loggerFactory), jsnlogConfiguration);

            app.UseStaticFiles();

            app.UseForwardedHeaders();

            app.UseCors(x => x.AllowAnyOrigin().WithHeaders("accept", "authorization", "content-type", "origin").AllowAnyMethod());

            app.UseXContentTypeOptions();
            app.UseXDownloadOptions();
            app.UseXfo(options => options.SameOrigin());
            app.UseXXssProtection(options => options.EnabledWithBlockMode());
            app.UseHsts(options => options.MaxAge(180).AllResponses().Preload().IncludeSubdomains());

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

            app.UseMultiTenant();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllerRoute(
                    name: "default",
                    pattern: "{controller=Home}/{action=Index}/{id?}");
                endpoints.MapRazorPages();
            });
        }
    }
}
