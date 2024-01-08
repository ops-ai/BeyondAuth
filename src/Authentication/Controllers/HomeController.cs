using Authentication.Filters;
using Authentication.Models;
using Finbuckle.MultiTenant;
using Identity.Core;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Raven.Client.Documents.Session;
using System.Reflection;

namespace Authentication.Controllers
{
    [SecurityHeaders]
    [Route("")]
    public class HomeController : BaseController
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IWebHostEnvironment _environment;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(
            IAsyncDocumentSession dbSession,
            IIdentityServerInteractionService interaction,
            IWebHostEnvironment environment,
            IHttpContextAccessor httpContextAccessor,
            UserManager<ApplicationUser> userManager) : base(dbSession)
        {
            _interaction = interaction;
            _environment = environment;
            _httpContextAccessor = httpContextAccessor;
            _userManager = userManager;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var sub = User.GetSubjectId();
            var user = await _userManager.FindByIdAsync(sub);
            if (user.DefaultApp != null)
                return Redirect(user.DefaultApp);

            var tenantSettings = _httpContextAccessor.HttpContext.GetMultiTenantContext<TenantSetting>()?.TenantInfo;
            //if (tenantSettings.AccountOptions.DashboardUrl != null)
            //    return Redirect(tenantSettings.AccountOptions.DashboardUrl);

            return View(new { tenantSettings?.AccountOptions?.DashboardUrl });
        }

        /// <summary>
        /// 404 Page not found
        /// </summary>
        /// <returns></returns>
        [Route("404")]
        public IActionResult PageNotFound()
        {
            Response.StatusCode = 404;
            return View();
        }

        /// <summary>
        /// Device offline
        /// </summary>
        /// <returns></returns>
        [Route("offline")]
        public IActionResult Offline()
        {
            return View();
        }

        /// <summary>
        /// Shows the error page
        /// </summary>
        [AllowAnonymous]
        [Route("error")]
        public async Task<IActionResult> Error(string errorId)
        {
            var vm = new ErrorViewModel();

            var version = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
            if (version.Length > 32)
                version = version[0..^32];
            vm.Version = $"{version}-{_environment.EnvironmentName}";

            var exceptionHandlerPathFeature = HttpContext.Features.Get<IExceptionHandlerPathFeature>();
            if (exceptionHandlerPathFeature != null)
            {
                if (exceptionHandlerPathFeature.Error is FileNotFoundException)
                    Response.StatusCode = 404;
            }

            // retrieve error details from identityserver
            var message = await _interaction.GetErrorContextAsync(errorId);
            if (message != null)
            {
                if (!_environment.IsDevelopment())
                    message.ErrorDescription = null; // only show in development
                vm.Error = message;
            }

            return View("Error", vm);
        }

        /// <summary>
        /// Shows the current version info
        /// </summary>
        [AllowAnonymous]
        [Route("version")]
        public IActionResult Version()
        {
            var version = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
            if (version.Length > 32)
                version = version[0..^32];
            return Content($"{version}-{_environment.EnvironmentName}");
        }
    }
}
