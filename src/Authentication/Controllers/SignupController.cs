// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Authentication.Extensions;
using Authentication.Filters;
using Authentication.Models.Account;
using Finbuckle.MultiTenant;
using Identity.Core;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Authentication.Controllers
{
    [SecurityHeaders]
    [AllowAnonymous]
    public class SignupController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IClientStore _clientStore;
        private readonly IAuthenticationSchemeProvider _schemeProvider;
        private readonly IEventService _events;
        private readonly IOptions<AccountOptions> _accountOptions;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<SignupController> _logger;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public SignupController(
            IAsyncDocumentSession dbSession,
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            IAuthenticationSchemeProvider schemeProvider,
            IEventService events,
            UserManager<ApplicationUser> userManager,
            IOptions<AccountOptions> accountOptions,
            IHttpContextAccessor httpContextAccessor,
            ILogger<SignupController> logger,
            SignInManager<ApplicationUser> signInManager) : base(dbSession)
        {
            _userManager = userManager;
            
            _interaction = interaction;
            _clientStore = clientStore;
            _schemeProvider = schemeProvider;
            _events = events;
            _accountOptions = accountOptions;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
            _signInManager = signInManager;
        }

        /// <summary>
        /// Entry point into the login workflow
        /// </summary>
        [HttpGet]
        [Route("signup")]
        public async Task<IActionResult> Signup(string returnUrl)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Handle postback from signup
        /// </summary>
        [HttpPost]
        [Route("signup")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Signup(LoginInputModel model, string button)
        {
            throw new NotImplementedException();
        }
    }
}
