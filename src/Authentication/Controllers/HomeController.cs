// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Authentication.Filters;
using Authentication.Models;
using Finbuckle.MultiTenant;
using Identity.Core;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents.Session;
using System;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;

namespace Authentication.Controllers
{
    [SecurityHeaders]
    public class HomeController : BaseController
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public HomeController(
            IAsyncDocumentSession dbSession, 
            IIdentityServerInteractionService interaction, 
            IWebHostEnvironment environment, 
            ILogger<HomeController> logger, 
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            UserManager<ApplicationUser> userManager) : base(dbSession)
        {
            _interaction = interaction;
            _environment = environment;
            _logger = logger;
            _configuration = configuration;
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
            if (tenantSettings.AccountOptions.DashboardUrl != null)
                return Redirect(tenantSettings.AccountOptions.DashboardUrl);

            return View();
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

        public static Color ParseColor(string cssColor)
        {
            cssColor = cssColor.Trim();

            if (cssColor.StartsWith("#"))
            {
                return ColorTranslator.FromHtml(cssColor);
            }
            else if (cssColor.StartsWith("rgb")) //rgb or argb
            {
                int left = cssColor.IndexOf('(');
                int right = cssColor.IndexOf(')');

                if (left < 0 || right < 0)
                    throw new FormatException("rgba format error");
                string noBrackets = cssColor.Substring(left + 1, right - left - 1);

                string[] parts = noBrackets.Split(',');

                int r = int.Parse(parts[0], CultureInfo.InvariantCulture);
                int g = int.Parse(parts[1], CultureInfo.InvariantCulture);
                int b = int.Parse(parts[2], CultureInfo.InvariantCulture);

                if (parts.Length == 3)
                {
                    return Color.FromArgb(r, g, b);
                }
                else if (parts.Length == 4)
                {
                    float a = float.Parse(parts[3], CultureInfo.InvariantCulture);
                    return Color.FromArgb((int)(a * 255), r, g, b);
                }
            }
            throw new FormatException("Not rgb, rgba or hexa color string");
        }

        /// <summary>
        /// Custom CSS
        /// </summary>
        /// <returns></returns>
        [Route("css/custom.css")]
        public IActionResult CustomCss()
        {
            var tenantSettings = _httpContextAccessor.HttpContext.GetMultiTenantContext<TenantSetting>()?.TenantInfo;

            if (tenantSettings.BrandingOptions.PrimaryColor == null)
                return Content("", "text/css");

            var rgba = ParseColor(tenantSettings.BrandingOptions.PrimaryColor);
            var css = $@"
.btn-primary {{
    background-color: {tenantSettings.BrandingOptions.PrimaryColor};
    border-color: {tenantSettings.BrandingOptions.PrimaryColor};
}}
.btn-primary:focus, .btn-primary:hover {{
    background-color: rgba({rgba.R},{rgba.G},{rgba.B},.75);
    border-color: {tenantSettings.BrandingOptions.PrimaryColor};
}}
.form-control:focus, .form-control:hover {{
    border-color: {tenantSettings.BrandingOptions.PrimaryColor};
    box-shadow: 0 0 0 .25rem rgba({rgba.R},{rgba.G},{rgba.B},.25);
}}
            ";

            return Content(css, "text/css");
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
                _logger.LogError(exceptionHandlerPathFeature.Error, "Uncaught exception {path}", exceptionHandlerPathFeature.Path);
            }

            // retrieve error details from identityserver
            var message = await _interaction.GetErrorContextAsync(errorId);
            if (message != null)
            {
                _logger.LogError("Identity Exception: {error}", message.Error);

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
        public IActionResult Version()
        {
            var version = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
            if (version.Length > 32)
                version = version[0..^32];
            return Content($"{version}-{_environment.EnvironmentName}");
        }
    }
}