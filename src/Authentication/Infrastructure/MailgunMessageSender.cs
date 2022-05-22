using Authentication.Models;
using Finbuckle.MultiTenant;
using Identity.Core;
using Identity.Core.Settings;
using IdentityModel;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using System.Security.Claims;
using System.Text.Json;

namespace Authentication.Infrastructure
{
    public class MailgunMessageSender : IEmailSender
    {
        private readonly ILogger _logger;
        private readonly IDocumentStore _store;
        private readonly IOptions<EmailOptions> _emailSettings;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public MailgunMessageSender(IDocumentStore store, IOptions<EmailOptions> emailSettings, ILoggerFactory factory, IHttpClientFactory httpClientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _logger = factory.CreateLogger<MailgunMessageSender>();
            _emailSettings = emailSettings;
            _store = store;
            _httpClientFactory = httpClientFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task SendEmailAsync(string toEmail, string toName, string templateId, IEnumerable<TemplateVariable> templateData, string fromName, string fromEmail,
            string subject, string? replyTo = null, List<string>? cc = null, List<string>? bcc = null)
        {
            try
            {
                using var httpClient = _httpClientFactory.CreateClient("mailgun");
                var tenantInfo = _httpContextAccessor.HttpContext.GetMultiTenantContext<TenantSetting>()?.TenantInfo;
                var vars = templateData.ToList();
                vars.Add(new TemplateVariable { Name = "logo", Value = tenantInfo.BrandingOptions?.Logo ?? "https://account.beyondauth.io/logo.png" });
                vars.Add(new TemplateVariable { Name = "primaryColor", Value = tenantInfo.BrandingOptions?.PrimaryColor ?? "#177CAB" });
                vars.Add(new TemplateVariable { Name = "secondaryColor", Value = tenantInfo.BrandingOptions?.SecondaryColor ?? "#177CAB" });

                var formContent = new FormUrlEncodedContent(new Dictionary<string, string> {
                 { "from", $"{fromName} <{fromEmail}>" },
                 { "to", $"{toName} <{toEmail}>" },
                 { "subject", subject  },
                 { "template", templateId },
                 { "h:Reply-To", $"{fromName} <{replyTo??fromEmail}>" },
                 { "h:X-Mailgun-Variables", JsonSerializer.Serialize(templateData.ToDictionary(d => d.Name, d => d.Value)) }
                 });

                var response = await httpClient.PostAsync($"{_emailSettings.Value.ApiBaseUrl}messages", formContent);
                if (!response.IsSuccessStatusCode)
                {
                    var error = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    _logger.LogError("Error sending email: {error}", error);
                }

                var mailGunResponse = JsonSerializer.Deserialize<MailGunResponseModel>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                });

                if (mailGunResponse == null)
                {
                    _logger.LogWarning("MailgunMessageSender: Failed to log sent email {toEmail}", toEmail);
                    return;
                }

                using (var session = _store.OpenAsyncSession($"TenantIdentity-{tenantInfo.Identifier}"))
                {
                    var newEmail = new SentEmail
                    {
                        Id = $"SentEmails/{mailGunResponse.Id[1..^1]}",
                        From = $"{fromName} <{fromEmail}>",
                        ReplyTo = $"{fromName} <{fromEmail}>",
                        To = new List<string> { toEmail },
                        Subject = subject,
                        TemplateId = templateId,
                        TemplateData = templateData.ToDictionary(t => t.Name, t => t.Sensitive ? "****" : t.Value),
                        Cc = cc,
                        UserId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(JwtClaimTypes.Subject),
                        RefId = mailGunResponse.Id
                    };
                    await session.StoreAsync(newEmail);
                    session.Advanced.GetMetadataFor(newEmail)["@expires"] = DateTime.UtcNow.AddDays(60);
                    await session.SaveChangesAsync();
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Sending email failed");

                throw;
            }
        }

        public async Task SendEmailWithAttachmentAsync(string toEmail, string toName, string templateId, IEnumerable<TemplateVariable> templateData, string fromEmail, string fromName, EmailAttachmentModel? attachment,
            string subject)
        {
            try
            {
                using var httpClient = _httpClientFactory.CreateClient("mailgun");

                var multipart = new MultipartFormDataContent
                {
                    { new StringContent($"{fromName} <{fromEmail}>"), "from" },
                    { new StringContent($"{toName} <{toEmail}>"), "to" },
                    { new StringContent(subject), "subject" },
                    { new StringContent(templateId), "template" },
                    { new StringContent($"{fromName} <{fromEmail}>"), "h:Reply-To" },
                    { new StringContent(JsonSerializer.Serialize(templateData.ToDictionary(d => d.Name, d => d.Value))), "h:X-Mailgun-Variables" }
                };

                if (attachment != null && attachment.File != null)
                {
                    var fileContent = new ByteArrayContent(attachment.File);

                    fileContent.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("form-data")
                    {
                        Name = "attachment",
                        FileName = attachment.FileName
                    };
                    multipart.Add(fileContent);
                }

                var response = await httpClient.PostAsync($"{_emailSettings.Value.ApiBaseUrl}messages", multipart);
                if (!response.IsSuccessStatusCode)
                {
                    throw new SendNotificationException($"Sending email failed: {toEmail} {templateId} {JsonSerializer.Serialize(templateData)}; " +
                        $"Error message {await response.Content.ReadAsStringAsync()}");
                }

                var mailGunResponse = JsonSerializer.Deserialize<MailGunResponseModel>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                });

                if (mailGunResponse == null)
                {
                    _logger.LogWarning("MailgunMessageSender: Failed to log sent email {toEmail}", toEmail);
                    return;
                }

                var tenantInfo = _httpContextAccessor.HttpContext.GetMultiTenantContext<TenantSetting>()?.TenantInfo;
                using (var session = _store.OpenAsyncSession($"TenantIdentity-{tenantInfo.Identifier}"))
                {
                    var newEmail = new SentEmail
                    {
                        Id = $"SentEmails/{mailGunResponse.Id[1..^1]}",
                        From = $"{fromName} <{fromEmail}>",
                        ReplyTo = $"{fromName} <{fromEmail}>",
                        To = new List<string> { toEmail },
                        Subject = subject,
                        TemplateId = templateId,
                        TemplateData = templateData.ToDictionary(t => t.Name, t => t.Sensitive ? "****" : t.Value),
                        UserId = _httpContextAccessor.HttpContext?.User?.FindFirstValue(JwtClaimTypes.Subject),
                        RefId = mailGunResponse.Id
                    };
                    await session.StoreAsync(newEmail);
                    session.Advanced.GetMetadataFor(newEmail)["@expires"] = DateTime.UtcNow.AddDays(60);
                    await session.SaveChangesAsync();
                }
            }
            catch (SendNotificationException ex)
            {
                _logger.LogCritical(ex, "Sending email failed");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"MailgunMessageSender: Sending email failed");
                throw;
            }
        }
    }
}
