using Audit.Core;
using Authentication.Domain;
using Authentication.Filters;
using Authentication.Infrastructure;
using Authentication.Models;
using Authentication.Models.Messages;
using Authentication.Models.PasswordReset;
using Identity.Core;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Raven.Client.Documents.Session;
using System.Net;
using System.Text.RegularExpressions;

namespace Authentication.Controllers
{
    [SecurityHeaders]
    [AllowAnonymous]
    public class PasswordResetController : BaseController
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IClientStore _clientStore;
        private readonly IAuthenticationSchemeProvider _schemeProvider;
        private readonly IEventService _events;
        private readonly IOptions<AccountOptions> _accountOptions;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger _logger;
        private readonly IdentityErrorDescriber _identityErrorDescriber;
        private readonly IEmailSender _emailSender;

        public PasswordResetController(
            IAsyncDocumentSession dbSession,
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            IAuthenticationSchemeProvider schemeProvider,
            IEventService events,
            UserManager<ApplicationUser> userManager,
            IOptions<AccountOptions> accountOptions,
            IHttpContextAccessor httpContextAccessor,
            ILogger<PasswordResetController> logger,
            IdentityErrorDescriber identityErrorDescriber,
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
            _identityErrorDescriber = identityErrorDescriber;
            _emailSender = emailSender;
        }

        /// <summary>
        /// Forgot password
        /// </summary>
        /// <param name="returnUrl"></param>
        /// <param name="email"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("forgot-password")]
        public async Task<IActionResult> ForgotPassword(string returnUrl, string email)
        {
            try
            {
                var interaction = await _interaction.GetAuthorizationContextAsync(returnUrl);
                if (interaction != null)
                    ViewBag.Logo = interaction.Client.LogoUri;

                return View(new ForgotPasswordModel { ReturnUrl = returnUrl, Email = email });
            }
            catch (Exception ex)
            {
                _logger.LogError(500, ex, "Error displaying forgot password page");
                throw;
            }
        }

        /// <summary>
        /// Process forgot password request
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("forgot-password")]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordModel model)
        {
            try
            {
                if (!ModelState.IsValid) return View(model);
                var user = await _userManager.FindByNameAsync(model.Email);

                // Don't reveal that the user does not exist or is not confirmed
                Thread.Sleep(new Random(1).Next(500, 2000));

                ViewData["Email"] = model.Email;

                if (user == null)
                {
                    ViewData["ReturnUrl"] = model.ReturnUrl;
                    _logger.LogWarning(10, "User {model.Email} tried to reset their password, but the user was not found.", model.Email);

                    return View("ForgotPasswordNotAllowed");
                }

                if (!user.PasswordResetAllowed)
                {
                    _logger.LogWarning(11, "User {model.Email} tried to reset their password, but password reset is not allowed for this user", model.Email);
                    ViewData["ReturnUrl"] = model.ReturnUrl;
                    return View("ForgotPasswordNotAllowed");
                }

                _logger.LogInformation(12, "User {model.Email} requested a password reset email.", model.Email);

                // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=532713
                // Send an email with this link
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);

                // Url.Action auto encodes params you pass to it, but ReturnUrl is already encoded. Don't double encode
                var callbackUrl = Url.Action("ResetPassword", "PasswordReset", new { code, returnUrl = model.ReturnUrl }, HttpContext.Request.Scheme);

                var interaction = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);
                string supportEmail;
                if (interaction != null)
                {
                    supportEmail = interaction.Client.Properties.ContainsKey("SupportEmail") ? interaction.Client.Properties["SupportEmail"] : _accountOptions.Value.SupportEmail ?? "support@beyondauth.io";
                    ViewBag.Logo = interaction.Client.LogoUri;
                }
                else
                    supportEmail = _accountOptions.Value.SupportEmail ?? "support@beyondauth.io";

                await _emailSender.SendEmailAsync(user.Email, user.FirstName, "password-reset", new[] { new TemplateVariable { Name = "firstName", Value = user.FirstName }, new TemplateVariable { Name = "callbackUrl", Value = callbackUrl, Sensitive = true }, new TemplateVariable { Name = "supportEmail", Value = supportEmail, Sensitive = false } }, "BeyondAuth", "noreply@noreply.beyondauth.io", "Reset Password", clientEntity: interaction?.Client);
                await AuditScope.LogAsync("User:Password Reset Requested", new { SubjectId = user.Id });

                // If we got this far, something failed, redisplay form
                return View("ForgotPasswordConfirmation");
            }
            catch (Exception ex)
            {
                _logger.LogError(500, ex, "Error during forgot password");
                throw;
            }
        }

        /// <summary>
        /// Forgot password confirmation page
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        [Route("password-reset-sent")]
        public async Task<IActionResult> ForgotPasswordConfirmation(string? returnUrl)
        {
            var interaction = await _interaction.GetAuthorizationContextAsync(returnUrl);
            if (interaction != null)
            {
                ViewBag.Logo = interaction.Client.LogoUri;
            }

            return View();
        }


        /// <summary>
        /// GET: /Account/ResetPassword
        /// </summary>
        /// <param name="code"></param>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("reset-password")]
        public async Task<IActionResult> ResetPassword(string code = null, string returnUrl = null)
        {
            if (code == null)
                return RedirectToAction("Error", "Home");

            var interaction = await _interaction.GetAuthorizationContextAsync(returnUrl);
            if (interaction != null)
            {
                ViewBag.Logo = interaction.Client.LogoUri;
            }

            return View(new ResetPasswordModel { ReturnUrl = returnUrl });
        }

        /// <summary>
        /// Process reset password request
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("reset-password")]
        public async Task<IActionResult> ResetPassword(ResetPasswordModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                    return View(model);

                var user = await _userManager.FindByNameAsync(model.Email);

                Thread.Sleep(new Random(1).Next(500, 2000));

                if (user == null)
                {
                    // Don't reveal that the user does not exist
                    AddErrors(IdentityResult.Failed(_identityErrorDescriber.InvalidToken()));
                    return View(model);
                }

                var resetPasswordStatus = await _userManager.ResetPasswordAsync(user, model.Code, model.Password);
                if (!resetPasswordStatus.Succeeded)
                {
                    await AuditScope.LogAsync($"User:Passowrd Reset Failure", new { SubjectId = user.Id, resetPasswordStatus.Errors });
                    AddErrors(resetPasswordStatus);
                    return View(model);
                }

                _logger.LogInformation(13, "User {model.Email} reset their password. Sending password change confirmation email", model.Email);

                var interaction = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);
                var supportEmail = "support@beyondauth.io";
                if (interaction != null)
                {
                    supportEmail = interaction.Client.Properties.ContainsKey("SupportEmail") ? interaction.Client.Properties["SupportEmail"] : _accountOptions.Value.SupportEmail ?? "support@beyondauth.io";
                    ViewBag.Logo = interaction.Client?.LogoUri;
                }

                await _emailSender.SendEmailAsync(user.Email, user.FirstName, "password-reset-confirmation", new[] { new TemplateVariable { Name = "firstName", Value = user.FirstName }, new TemplateVariable { Name = "supportEmail", Value = supportEmail, Sensitive = false } }, "BeyondAuth", "noreply@noreply.beyondauth.io", "Password Reset Confirmation", clientEntity: interaction?.Client);
                await AuditScope.LogAsync($"User:Passowrd Reset Successfully", new { SubjectId = user.Id });

                return RedirectToAction(nameof(ResetPasswordConfirmation), "PasswordReset", new { returnUrl = model.ReturnUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(500, ex, "Error during reset password");

                ModelState.AddModelError("", ex.Message);

                return View(model);
            }
        }

        private void AddErrors(IdentityResult result)
        {
            var rgx = new Regex("\\(Exception from HRESULT: 0x\\d+\\)");

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError("Password", rgx.Replace(error.Description, "").TrimEnd());
            }
        }

        /// <summary>
        /// Reset password confirmation page
        /// </summary>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("reset-password-successful")]
        public async Task<IActionResult> ResetPasswordConfirmation(string returnUrl)
        {
            ViewData["ReturnUrl"] = returnUrl;

            var interaction = await _interaction.GetAuthorizationContextAsync(returnUrl);
            if (interaction != null)
            {
                ViewBag.Logo = interaction.Client?.LogoUri;
            }

            return View();
        }
    }
}
