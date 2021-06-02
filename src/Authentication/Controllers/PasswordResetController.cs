using Authentication.Domain;
using Authentication.Filters;
using Authentication.Infrastructure;
using Authentication.Models.Messages;
using Authentication.Models.PasswordReset;
using Identity.Core;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raven.Client.Documents.Session;
using System;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

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
        private readonly IEmailService _emailService;
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
            IEmailService emailService,
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
            _emailService = emailService;
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
        public IActionResult ForgotPassword(string returnUrl, string email)
        {
            try
            {
                returnUrl = WebUtility.UrlEncode(returnUrl);
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

                if (user == null)
                {
                    ViewData["ReturnUrl"] = model.ReturnUrl;
                    _logger.LogWarning(10, $"User {model.Email} tried to reset their password, but the user was not found.");

                    return View("ForgotPasswordNotAllowed");
                }

                if (!user.PasswordResetAllowed)
                {
                    _logger.LogWarning(11, $"User {model.Email} tried to reset their password, but password reset is not allowed for this user");
                    ViewData["ReturnUrl"] = model.ReturnUrl;
                    return View("ForgotPasswordNotAllowed");
                }

                _logger.LogInformation(12, $"User {model.Email} requested a password reset email.");

                // For more information on how to enable account confirmation and password reset please visit http://go.microsoft.com/fwlink/?LinkID=532713
                // Send an email with this link
                var code = await _userManager.GeneratePasswordResetTokenAsync(user);

                // Url.Action auto encodes params you pass to it, but ReturnUrl is already encoded. Don't double encode
                var callbackUrl = Url.Action("ResetPassword", "PasswordReset", new { code, returnUrl = WebUtility.UrlDecode(model.ReturnUrl) }, HttpContext.Request.Scheme);

                var emailMessage = new ResetPasswordEmailMessage { To = model.Email, FirstName = user.FirstName, CallbackUrl = callbackUrl };

                var interaction = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);
                if (interaction != null)
                {
                    var clientSetting = interaction.Client as ClientEntity;
                    emailMessage.SupportEmail = clientSetting?.SupportEmail ?? _accountOptions.Value.SupportEmail ?? "support@beyondauth.io";
                }

                var bodyHtml = await _emailService.RenderPartialViewToString("ResetPassword.html", emailMessage, null);
                var bodyTxt = await _emailService.RenderPartialViewToString("ResetPassword.txt", emailMessage, null);
                await _emailSender.SendEmailAsync(emailMessage.To, "Password reset", bodyHtml, bodyTxt);

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
        public IActionResult ForgotPasswordConfirmation()
        {
            try
            {
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(500, ex, "Error displaying forgot password confirmation page");
                throw;
            }
        }


        /// <summary>
        /// GET: /Account/ResetPassword
        /// </summary>
        /// <param name="code"></param>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        [HttpGet]
        [Route("reset-password")]
        public IActionResult ResetPassword(string code = null, string returnUrl = null)
        {
            try
            {
                if (code == null)
                    return RedirectToAction("Error", "Home");

                return View(new ResetPasswordModel { ReturnUrl = returnUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(500, ex, "Error displaying reset password page");
                throw;
            }
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
                    AddErrors(resetPasswordStatus);
                    return View(model);
                }

                _logger.LogInformation(13, $"User {model.Email} reset their password. Sending password change confirmation email");

                var emailMessage = new ResetPasswordConfirmationEmailMessage { To = user.Email, FirstName = user.FirstName };

                var interaction = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);
                if (interaction != null)
                {
                    //var clientSetting = interaction.Client as ClientEntity;
                    //emailMessage.SupportEmail = clientSetting?.SupportEmail ?? _accountOptions.Value.SupportEmail ?? "support@beyondauth.io";
                }

                var bodyHtml = await _emailService.RenderPartialViewToString("ResetPasswordConfirmation.html", emailMessage, null);
                var bodyTxt = await _emailService.RenderPartialViewToString("ResetPasswordConfirmation.txt", emailMessage, null);

                await _emailSender.SendEmailAsync(user.Email, "Password Reset Confirmation", bodyHtml, bodyTxt);

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
        public IActionResult ResetPasswordConfirmation(string returnUrl)
        {
            try
            {
                ViewData["ReturnUrl"] = returnUrl;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(500, ex, "Error displaying reset password confirmation");
                throw;
            }
        }
    }
}
