// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Audit.Core;
using Authentication.Extensions;
using Authentication.Filters;
using Authentication.Infrastructure;
using Authentication.Models;
using Authentication.Models.Account;
using Authentication.Models.Messages;
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
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using Raven.Client.Documents.Session;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Authentication.Controllers
{
    [SecurityHeaders]
    [AllowAnonymous]
    public class AccountController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IClientStore _clientStore;
        private readonly IAuthenticationSchemeProvider _schemeProvider;
        private readonly IEventService _events;
        private readonly IOptions<AccountOptions> _accountOptions;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger _logger;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly IEmailSender _emailSender;

        public AccountController(
            IAsyncDocumentSession dbSession,
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            IAuthenticationSchemeProvider schemeProvider,
            IEventService events,
            UserManager<ApplicationUser> userManager,
            IOptions<AccountOptions> accountOptions,
            IHttpContextAccessor httpContextAccessor,
            ILogger<AccountController> logger,
            SignInManager<ApplicationUser> signInManager,
            IEmailSender emailSender) : base(dbSession)
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
            _emailSender = emailSender;
        }

        /// <summary>
        /// Entry point into the login workflow
        /// </summary>
        [HttpGet("login")]
        public async Task<IActionResult> Login(string returnUrl, [FromServices] IOtacManager otacManager)
        {
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
            if (context?.AcrValues?.Any(t => t.StartsWith("otac:", StringComparison.OrdinalIgnoreCase)) == true)
            {
                //try to auto log the user in
                var otac = context.AcrValues.First(t => t.StartsWith("otac:", StringComparison.OrdinalIgnoreCase)).Split(':').Last();

                var (status, user) = await otacManager.ValidateOtacAsync(otac); //this is a destructive operation
                if (status.Succeeded && user != null && !user.Disabled && (user.AccountExpiration == null || user.AccountExpiration > DateTime.UtcNow))
                {
                    await _events.RaiseAsync(new UserLoginSuccessEvent(user.Email, user.Id, user.DisplayName, clientId: context?.Client.ClientId));

                    // only set explicit expiration here if user chooses "remember me". 
                    // otherwise we rely upon expiration configured in cookie middleware.
                    var props = new AuthenticationProperties
                    {
                        IsPersistent = false,
                        ExpiresUtc = null,
                        RedirectUri = !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : "~/"
                    };

                    var tenantSettings = _httpContextAccessor.HttpContext.GetMultiTenantContext<TenantSetting>()?.TenantInfo;
                    var isuser = new IdentityServerUser(user.Id)
                    {
                        DisplayName = user.DisplayName,
                        AuthenticationMethods = new List<string> { "otp" },
                        IdentityProvider = $"https://{tenantSettings.Identifier}"
                    };
                    //TODO: props.Items.Add("browser_id", model.BrowserId);

                    await HttpContext.SignInAsync(isuser, props);

                    if (context != null)
                    {
                        if (context.IsNativeClient())
                        {
                            // The client is native, so this change in how to
                            // return the response is for better UX for the end user.
                            return this.LoadingPage("Redirect", returnUrl);
                        }

                        // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                        return Redirect(returnUrl);
                    }

                    return Redirect(props.RedirectUri);
                }
            }

            // build a model so we know what to show on the login page
            var vm = await BuildLoginViewModelAsync(returnUrl, context);

            if (vm.IsExternalLoginOnly)
                // we only have one option for logging in and it's an external provider
                return RedirectToAction("Challenge", "External", new { provider = vm.ExternalLoginScheme, returnUrl });

            return View(vm);
        }

        /// <summary>
        /// Handle postback from username/password login
        /// </summary>
        [HttpPost("login")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginInputModel model, string button)
        {
            // check if we are in the context of an authorization request
            var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);

            // the user clicked the "cancel" button
            if (button != "login")
                if (context != null)
                {
                    // if the user cancels, send a result back into IdentityServer as if they 
                    // denied the consent (even if this client does not require consent).
                    // this will send back an access denied OIDC error response to the client.
                    await _interaction.DenyAuthorizationAsync(context, AuthorizationError.AccessDenied);

                    // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                    if (context.IsNativeClient())
                    {
                        // The client is native, so this change in how to
                        // return the response is for better UX for the end user.
                        return this.LoadingPage("Redirect", model.ReturnUrl);
                    }

                    return Redirect(model.ReturnUrl);
                }
                else
                    // since we don't have a valid context, then we just go back to the home page
                    return Redirect("~/");

            if (ModelState.IsValid)
            {
                if (!model.Email.Contains('@') && !string.IsNullOrEmpty(_accountOptions.Value.DefaultDomain))
                    model.Email += $"@{_accountOptions.Value.DefaultDomain}";

                // validate username/password against in-memory store
                var user = await _userManager.FindByNameAsync(model.Email);
                if (user == null)
                {
                    await _events.RaiseAsync(new UserLoginFailureEvent(model.Email, "user doesn't exist", clientId: context?.Client.ClientId));
                    ModelState.AddModelError(nameof(model.Password), _accountOptions.Value.InvalidCredentialsErrorMessage);
                }
                else if (user.Disabled)
                {
                    await _events.RaiseAsync(new UserLoginFailureEvent(model.Email, "user disabled", clientId: context?.Client.ClientId));
                    ModelState.AddModelError(nameof(model.Password), _accountOptions.Value.InvalidCredentialsErrorMessage);
                }
                else if (user.AccountExpiration != null && user.AccountExpiration > DateTime.UtcNow)
                {
                    await _events.RaiseAsync(new UserLoginFailureEvent(model.Email, "account expired", clientId: context?.Client.ClientId));
                    ModelState.AddModelError(nameof(model.Password), _accountOptions.Value.InvalidCredentialsErrorMessage);
                }
                else if (!_accountOptions.Value.AllowLocalLogin)
                {
                    await _events.RaiseAsync(new UserLoginFailureEvent(model.Email, "local login not allowed", clientId: context?.Client.ClientId));
                    ModelState.AddModelError(nameof(model.Email), "Local login not allowed");
                }
                else
                {
                    var signinResult = await _signInManager.CheckPasswordSignInAsync(user, model.Password, true);
                    if (signinResult.Succeeded)
                    {
                        await _events.RaiseAsync(new UserLoginSuccessEvent(user.Email, user.Id, user.DisplayName, clientId: context?.Client.ClientId));

                        // only set explicit expiration here if user chooses "remember me". 
                        // otherwise we rely upon expiration configured in cookie middleware.
                        var props = new AuthenticationProperties
                        {
                            IsPersistent = _accountOptions.Value.AllowRememberLogin && model.RememberLogin,
                            ExpiresUtc = _accountOptions.Value.AllowRememberLogin && model.RememberLogin ? DateTimeOffset.UtcNow.Add(_accountOptions.Value.RememberMeLoginDuration) : null,
                            RedirectUri = !string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl) ? model.ReturnUrl : "~/"
                        };
                        props.Items.Add("browser_id", model.BrowserId);
                        var tenantSettings = _httpContextAccessor.HttpContext.GetMultiTenantContext<TenantSetting>()?.TenantInfo;
                        var isuser = new IdentityServerUser(user.Id)
                        {
                            DisplayName = user.DisplayName,
                            AuthenticationMethods = new List<string> { "pwd" },
                            IdentityProvider = $"https://{tenantSettings.Identifier}"
                        };
                        //TODO: check for MFA add to Authentication Methods https://datatracker.ietf.org/doc/html/rfc8176
                        
                        await HttpContext.SignInAsync(isuser, props);

                        if (user.ChangePasswordOnNextLogin)
                            return RedirectToAction("ChangePassword");

                        if (context != null)
                        {
                            if (context.IsNativeClient())
                            {
                                // The client is native, so this change in how to
                                // return the response is for better UX for the end user.
                                return this.LoadingPage("Redirect", model.ReturnUrl);
                            }

                            // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                            return Redirect(model.ReturnUrl);
                        }

                        return Redirect(props.RedirectUri);
                    }
                    else if (signinResult.IsLockedOut && _accountOptions.Value.EnableLockedOutMessage) //Handle locked out message propagation if allowed by tenant settings
                    {
                        await _events.RaiseAsync(new UserLoginFailureEvent(model.Email, "locked out", clientId: context?.Client.ClientId));
                        ModelState.AddModelError(nameof(model.Email), _accountOptions.Value.LockedOutErrorMessage);
                    }
                    else if (signinResult.IsNotAllowed) //Handle local login not allowed for user signinResult.IsNotAllowed
                    {
                        await _events.RaiseAsync(new UserLoginFailureEvent(model.Email, "not allowed", clientId: context?.Client.ClientId));
                        ModelState.AddModelError(nameof(model.Email), "local login is not allowed");
                    }
                    else if (signinResult.RequiresTwoFactor)
                    {
                        //TODO: Handle signinResult.RequiresTwoFactor
                        throw new NotSupportedException("two-factor not supported");
                    }
                    else
                    {
                        await _events.RaiseAsync(new UserLoginFailureEvent(model.Email, "invalid credentials", clientId: context?.Client.ClientId));
                        ModelState.AddModelError(nameof(model.Password), _accountOptions.Value.InvalidCredentialsErrorMessage);
                    }
                }
            }

            // something went wrong, show form with error
            var vm = await BuildLoginViewModelAsync(model, context);
            return View(vm);
        }


        /// <summary>
        /// Show logout page
        /// </summary>
        [HttpGet("logout")]
        public async Task<IActionResult> Logout(string logoutId)
        {
            // build a model so the logout page knows what to display
            var vm = await BuildLogoutViewModelAsync(logoutId);

            if (vm.ShowLogoutPrompt == false)
                // if the request for logout was properly authenticated from IdentityServer, then
                // we don't need to show the prompt and can just log the user out directly.
                return await Logout(vm);

            return View(vm);
        }

        /// <summary>
        /// Handle logout page postback
        /// </summary>
        [HttpPost("logout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout(LogoutInputModel model)
        {
            // build a model so the logged out page knows what to display
            var vm = await BuildLoggedOutViewModelAsync(model.LogoutId);

            if ((await HttpContext.AuthenticateAsync()).Succeeded)
            {
                // delete local authentication cookie
                await _signInManager.SignOutAsync();

                // raise the logout event
                await _events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));
            }

            // check if we need to trigger sign-out at an upstream identity provider
            if (vm.TriggerExternalSignout)
            {
                // build a return URL so the upstream provider will redirect back
                // to us after the user has logged out. this allows us to then
                // complete our single sign-out processing.
                var url = Url.Action("Logout", new { logoutId = vm.LogoutId });

                // this triggers a redirect to the external provider for sign-out
                return SignOut(new AuthenticationProperties { RedirectUri = url }, vm.ExternalAuthenticationScheme);
            }

            return View("LoggedOut", vm);
        }

        [HttpGet]
        public IActionResult AccessDenied() => View();


        /*****************************************/
        /* helper APIs for the AccountController */
        /*****************************************/
        private async Task<LoginViewModel> BuildLoginViewModelAsync(string returnUrl, AuthorizationRequest? context)
        {
            if (context?.IdP != null && await _schemeProvider.GetSchemeAsync(context.IdP) != null)
            {
                var local = context.IdP == IdentityServerConstants.LocalIdentityProvider;


                // this is meant to short circuit the UI and only trigger the one external IdP
                var vm = new LoginViewModel
                {
                    EnableLocalLogin = local,
                    ReturnUrl = returnUrl,
                    Email = context?.LoginHint,
                    SignupUrl = _accountOptions.Value.SignupUrl,
                    SignupMessage = _accountOptions.Value.SignupMessage,
                    SignupText = _accountOptions.Value.SignupText
                };

                if (!local)
                    vm.ExternalProviders = new[] { new ExternalProvider { AuthenticationScheme = context.IdP } };

                return vm;
            }

            var schemes = await _schemeProvider.GetAllSchemesAsync();
            var tenantSettings = _httpContextAccessor.HttpContext.GetMultiTenantContext<TenantSetting>()?.TenantInfo;

            //var providers = new List<ExternalProvider>();
            //providers.Add(new ExternalProvider { AuthenticationScheme = GoogleDefaults.AuthenticationScheme, DisplayName = "Google" });
            var providers = tenantSettings.ExternalIdps.Where(s => s.Enabled)
                .Select(x => new ExternalProvider
                {
                    DisplayName = x.Name,
                    AuthenticationScheme = x.Scheme
                }).ToList();

            var allowLocal = true;
            if (context?.Client.ClientId != null)
            {
                var client = await _clientStore.FindEnabledClientByIdAsync(context?.Client.ClientId);
                if (client != null)
                {
                    allowLocal = client.EnableLocalLogin;

                    if (client.IdentityProviderRestrictions != null && client.IdentityProviderRestrictions.Any())
                        providers = providers.Where(provider => client.IdentityProviderRestrictions.Contains(provider.AuthenticationScheme)).ToList();
                }
            }

            return new LoginViewModel
            {
                AllowRememberLogin = _accountOptions.Value.AllowRememberLogin,
                AllowPasswordReset = _accountOptions.Value.AllowPasswordReset,
                EnableLocalLogin = allowLocal && _accountOptions.Value.AllowLocalLogin,
                ReturnUrl = returnUrl,
                Email = context?.LoginHint,
                ExternalProviders = providers.ToArray(),
                SignupUrl = _accountOptions.Value.SignupUrl,
                SignupMessage = _accountOptions.Value.SignupMessage,
                SignupText = _accountOptions.Value.SignupText
            };
        }

        private async Task<LoginViewModel> BuildLoginViewModelAsync(LoginInputModel model, AuthorizationRequest? context)
        {
            var vm = await BuildLoginViewModelAsync(model.ReturnUrl, context);
            vm.Email = model.Email;
            vm.RememberLogin = model.RememberLogin;
            return vm;
        }

        private async Task<LogoutViewModel> BuildLogoutViewModelAsync(string logoutId)
        {
            var vm = new LogoutViewModel { LogoutId = logoutId, ShowLogoutPrompt = _accountOptions.Value.ShowLogoutPrompt };

            if (User?.Identity.IsAuthenticated != true)
            {
                // if the user is not authenticated, then just show logged out page
                vm.ShowLogoutPrompt = false;
                return vm;
            }

            var context = await _interaction.GetLogoutContextAsync(logoutId);
            _logger.LogInformation("logoutContext: " + JsonSerializer.Serialize(context));
            if (context?.ShowSignoutPrompt == false)
            {
                // it's safe to automatically sign-out
                vm.ShowLogoutPrompt = false;
                return vm;
            }

            // show the logout prompt. this prevents attacks where the user
            // is automatically signed out by another malicious web page.
            return vm;
        }

        private async Task<LoggedOutViewModel> BuildLoggedOutViewModelAsync(string logoutId)
        {
            // get context information (client name, post logout redirect URI and iframe for federated signout)
            var logout = await _interaction.GetLogoutContextAsync(logoutId);

            var vm = new LoggedOutViewModel
            {
                AutomaticRedirectAfterSignOut = _accountOptions.Value.AutomaticRedirectAfterSignOut,
                PostLogoutRedirectUri = logout?.PostLogoutRedirectUri,
                ClientName = string.IsNullOrEmpty(logout?.ClientName) ? logout?.ClientId : logout?.ClientName,
                SignOutIframeUrl = logout?.SignOutIFrameUrl,
                LogoutId = logoutId
            };

            if (User?.Identity.IsAuthenticated == true)
            {
                var idp = User.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;
                if (idp != null && idp != IdentityServerConstants.LocalIdentityProvider)
                {
                    var providerSupportsSignout = await HttpContext.GetSchemeSupportsSignOutAsync(idp);
                    if (providerSupportsSignout)
                    {
                        if (vm.LogoutId == null)
                            // if there's no current logout context, we need to create one
                            // this captures necessary info from the current logged in user
                            // before we signout and redirect away to the external IdP for signout
                            vm.LogoutId = await _interaction.CreateLogoutContextAsync();

                        vm.ExternalAuthenticationScheme = idp;
                    }
                }
            }

            return vm;
        }

        /// <summary>
        /// Change password
        /// </summary>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        [Authorize]
        [HttpGet("change-password")]
        public async Task<IActionResult> ChangePassword(string returnUrl)
        {
            try
            {
                await Task.FromResult(0);
                return View(new ChangePasswordViewModel { ReturnUrl = returnUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(500, ex, "Error displaying change password page");
                throw;
            }
        }

        /// <summary>
        /// Change password
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("change-password")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(model);

                var sub = User.GetSubjectId();
                var user = await _userManager.FindByIdAsync(sub);
                if (user != null && user.ChangePasswordAllowed)
                {
                    if (user.ChangePasswordOnNextLogin)
                        user.ChangePasswordOnNextLogin = false;

                    var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);
                    if (result.Succeeded)
                    {
                        _logger.LogInformation(6, "User changed their password successfully.");

                        await _emailSender.SendEmailAsync(user.Email, user.FirstName, "password-change-confirmation", new[] { new TemplateVariable { Name = "firstName", Value = user.FirstName } }, "BeyondAuth", "noreply@noreply.beyondauth.io", "Password Changed Confirmation");
                        await AuditScope.LogAsync($"User:Passowrd Changed", new { SubjectId = user.Id });

                        return View("ChangePasswordConfirmation");
                    }

                    AddErrors(result);
                    await AuditScope.LogAsync($"User:Passowrd Change Failure", new { SubjectId = user.Id, result.Errors });
                }

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(500, ex, "Error changing password");
                throw;
            }
        }

        private void AddErrors(IdentityResult result)
        {
            var rgx = new Regex("\\(Exception from HRESULT: 0x\\d+\\)");

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(error.Code == "PasswordMismatch" ? "OldPassword" : "NewPassword", rgx.Replace(error.Description, "").TrimEnd());
            }
        }
    }
}
