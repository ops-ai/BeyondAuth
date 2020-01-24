using Authentication.Data;
using Authentication.Models;
using Autofac;
using CorrelationId.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Raven.Client.Documents;
using System.Linq;
using System.Security.Cryptography.X509Certificates;
using HealthChecks.UI.Client;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Net;
using IdentityServer4.Stores;
using Authentication.Extensions;
using IdentityServer4;
using Newtonsoft.Json;
using IdentityServer4.Stores.Serialization;
using Microsoft.AspNetCore.Http;
using System;
using Microsoft.AspNetCore.DataProtection;
using IdentityServer4.Services;
using IdentityServer4.Contrib.RavenDB.Services;
using IdentityServer4.Validation;
using IdentityServer4.Contrib.RavenDB.Stores;
using JSNLog;
using Microsoft.Extensions.Logging;
using IdentityServer.LdapExtension.Extensions;
using IdentityServer.LdapExtension.UserModel;

namespace Authentication
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
                Configuration["ProxyNodes"]?.Split(';').ToList().ForEach(t =>
                {
                    if (!string.IsNullOrEmpty(t))
                        options.KnownProxies.Add(IPAddress.Parse(t));
                });
            });

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

            var dataProtection = services.AddDataProtection()
                .SetApplicationName("auth.ops.ai");

            if (!string.IsNullOrEmpty(Configuration["DataProtection:KeyIdentifier"]))
                dataProtection
                    .ProtectKeysWithAzureKeyVault(Configuration["DataProtection:KeyIdentifier"], Configuration["DataProtection:ClientId"], Configuration["DataProtection:ClientSecret"])
                    .PersistKeysToAzureBlobStorage(new Uri(Configuration["DataProtection:StorageUri"]));


            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection")));
            services.AddIdentity<ApplicationUser, IdentityRole>(options => options.SignIn.RequireConfirmedAccount = true)
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddControllersWithViews();
            services.AddRazorPages();
            services.AddSameSiteCookiePolicy();

            services.AddSingleton((ctx) =>
            {
                var store = new DocumentStore
                {
                    Urls = new[] { Configuration["Raven:Url"] },
                    Database = Configuration["Raven:Database"],
                    Certificate = Configuration.GetSection("Raven:EncryptionEnabled").Get<bool>() ? new X509Certificate2(Configuration["Raven:CertFile"], Configuration["Raven:CertPassword"]) : null
                };
                store.Conventions.CustomizeJsonSerializer += (JsonSerializer serializer) =>
                {
                    serializer.Converters.Add(new ClaimConverter());
                    serializer.Converters.Add(new ClaimsPrincipalConverter());
                };
                return store.Initialize();
            });

            var healthChecks = services.AddHealthChecks()
                .AddRavenDB(setup => { setup.Urls = new[] { Configuration["Raven:Url"] }; setup.Database = Configuration["Raven:Database"]; setup.Certificate = Configuration.GetSection("Raven:EncryptionEnabled").Get<bool>() ? new X509Certificate2(Configuration["Raven:CertFile"], Configuration["Raven:CertPassword"]) : null; }, "ravendb")
                .AddIdentityServer(new Uri(Configuration["BaseUrl"]), "openid-connect");

            if (!string.IsNullOrEmpty(Configuration["DataProtection:KeyIdentifier"]))
                healthChecks.AddAzureKeyVault(options =>
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
            })
                //.AddSigningCredential()
                .AddPersistedGrantStore<RavenDBPersistedGrantStore>()
                .AddClientStore<RavenDBClientStore>()
                .AddResourceStore<RavenDBResourceStore>()
                .AddCorsPolicyService<CorsPolicyService>()
                .AddLdapUsers<OpenLdapAppUser>(Configuration.GetSection("IdentityServerLdap"), UserStore.InMemory)
                /*.AddAspNetIdentity<ApplicationUser>()*/;

            builder.AddDeveloperSigningCredential();

            services.AddAuthentication();
                //.AddGoogle(options =>
                //{
                //    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;

                //    // register your IdentityServer with Google at https://console.developers.google.com
                //    // enable the Google+ API
                //    // set the redirect URI to http://localhost:5000/signin-google
                //    options.ClientId = Configuration["ExternalIdps:Google:ClientId"];
                //    options.ClientSecret = Configuration["ExternalIdps:Google:ClientSecret"];
                //});
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseDatabaseErrorPage();
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
