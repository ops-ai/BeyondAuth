// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Authentication.Filters;
using Authentication.Models;
using IdentityServer4.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;
using System.Threading.Tasks;

namespace Authentication.Controllers
{
    [SecurityHeaders]
    public class HomeController : Controller
    {
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger _logger;
        private readonly IConfiguration _configuration;

        public HomeController(IIdentityServerInteractionService interaction, IWebHostEnvironment environment, ILogger<HomeController> logger, IConfiguration configuration)
        {
            _interaction = interaction;
            _environment = environment;
            _logger = logger;
            _configuration = configuration;
        }

        [Authorize]
        public IActionResult Index()
        {


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

        /// <summary>
        /// Shows the error page
        /// </summary>
        [AllowAnonymous]
        public async Task<IActionResult> Error(string errorId)
        {
            var vm = new ErrorViewModel();

            var version = Assembly.GetEntryAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
            if (version.Length > 32)
                version = version.Substring(0, version.Length - 32);
            vm.Version = $"{version}-{_environment.EnvironmentName}";

            // retrieve error details from identityserver
            var message = await _interaction.GetErrorContextAsync(errorId);
            if (message != null)
            {
                vm.Error = message;

                if (!_environment.IsDevelopment())
                    message.ErrorDescription = null; // only show in development
            }

            return View("Error", vm);
        }
    }
}