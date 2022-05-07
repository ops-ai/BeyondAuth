// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Authentication.Filters;
using Authentication.Models.Diagnostics;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FeatureManagement.Mvc;
using Raven.Client.Documents.Session;

namespace Authentication.Controllers
{
    [FeatureGate(Constants.FeatureFlags.Diagnostics)]
    [SecurityHeaders]
    [Authorize]
    public class DiagnosticsController : BaseController
    {
        private readonly ILogger _logger;

        public DiagnosticsController(IAsyncDocumentSession dbSession, ILogger<DiagnosticsController> logger) : base(dbSession)
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

                model.Headers = Request.Headers;
                model.RemoteIpAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString();

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error displaying diagnostics");
                throw;
            }
        }

        /// <summary>
        /// Diagnostics in json format
        /// </summary>
        [AllowAnonymous]
        [Route("diagnostics.json")]
        public async Task<IActionResult> Diagnostics()
        {
            var authResult = await HttpContext.AuthenticateAsync();

            return Json(new
            {
                Request.Headers,
                RemoteIpAddress = Request.HttpContext.Connection.RemoteIpAddress?.ToString(),
                Request.IsHttps,
                Request.HttpContext.Connection.LocalPort,
                Claims = authResult?.Principal?.Claims.Select(t => new { t.Type, t.Value }),
                authResult?.Properties?.Items
            });
        }
    }
}