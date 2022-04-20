using Authentication.Infrastructure;
using Authentication.Models.Messages;
using Identity.Core;
using Microsoft.AspNetCore.Identity;
using Raven.Client.Documents;

namespace Authentication.Services
{
    public class PasswordResetService : BackgroundService
    {
        private readonly IDocumentStore _store;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IEmailSender _emailSender;
        private readonly IEmailService _emailService;
        private readonly ILogger _logger;

        public PasswordResetService(IDocumentStore store, IEmailSender emailSender, IEmailService emailService, ILoggerFactory loggerFactory)
        {
            _store = store;
            _emailSender = emailSender;
            _emailService = emailService;
            _logger = loggerFactory.CreateLogger<PasswordResetService>();
        }

        private async Task SendPasswordAsync(string requestId, string tenant)
        {
            _logger.LogInformation("Processing password reset request {requestId} in {tenant}", requestId, tenant);
            try
            {
                using (var session = _store.OpenAsyncSession($"TenantIdentity-{tenant}"))
                {
                    var request = await session.Include<PasswordResetRequest>(t => t.UserId).LoadAsync<PasswordResetRequest>(requestId);
                    if (request == null || request.Handled)
                        return;

                    var user = await session.LoadAsync<ApplicationUser>(request.UserId);
                    if (user == null)
                        return;

                    var code = await _userManager.GeneratePasswordResetTokenAsync(user);

                    // Url.Action auto encodes params you pass to it, but ReturnUrl is already encoded. Don't double encode
                    var callbackUrl = $"https://{tenant}/reset-password?code={code}";

                    var emailMessage = new ResetPasswordEmailMessage { To = user.Email, FirstName = user.FirstName, CallbackUrl = callbackUrl };

                    var bodyHtml = await _emailService.RenderPartialViewToString("ResetPassword.html", emailMessage, null);
                    var bodyTxt = await _emailService.RenderPartialViewToString("ResetPassword.txt", emailMessage, null);
                    await _emailSender.SendEmailAsync(emailMessage.To, "Password reset", bodyHtml, bodyTxt);

                    request.Handled = true;
                    await session.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing password reset request");
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="stoppingToken"></param>
        /// <returns></returns>
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using (var session = _store.OpenAsyncSession())
            {
                //TODO: Subscribe to chnages on TenantSettings and start listening to chnages on new databases or use service bus

                var tenantSettings = await session.Query<TenantSetting>().ToListAsync(stoppingToken);
                foreach (var tenantSetting in tenantSettings)
                {
                    _store.Changes($"TenantIdentity-{tenantSetting.Identifier}").ForDocumentsInCollection<PasswordResetRequest>()
                        .Subscribe(change =>
                        {
                            if (change.Type == Raven.Client.Documents.Changes.DocumentChangeTypes.Put)
                                SendPasswordAsync(change.Id, tenantSetting.Identifier).ConfigureAwait(false).GetAwaiter().GetResult();
                        });
                }
            }

            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
