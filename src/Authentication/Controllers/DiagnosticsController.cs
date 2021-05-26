// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Authentication.Filters;
using Authentication.Models.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Authentication.Controllers
{
    [SecurityHeaders]
    [Authorize]
    public class DiagnosticsController : Controller
    {
        private readonly ILogger _logger;

        public DiagnosticsController(ILogger<DiagnosticsController> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var localAddresses = new string[] { "127.0.0.1", "::1", HttpContext.Connection.LocalIpAddress.ToString() };
                if (!localAddresses.Contains(HttpContext.Connection.RemoteIpAddress.ToString()))
                {
                    //return NotFound();
                }

                var model = new DiagnosticsViewModel(await HttpContext.AuthenticateAsync());
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error displaying diagnostics");
                throw;
            }
        }
    }
}