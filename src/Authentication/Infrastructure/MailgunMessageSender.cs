using Authentication.Models;
using Finbuckle.MultiTenant;
using Identity.Core;
using Identity.Core.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;

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

        public Task ReportIssueAsync(string subject, string txtMessage) => throw new System.NotImplementedException();

        public async Task SendEmailAsync(string toEmail, string toName, string templateId, object templateData, string fromName, string fromEmail,
            string subject, string? replyTo = null, List<string>? cc = null, List<string>? bcc = null)
        {
            try
            {
                using var httpClient = _httpClientFactory.CreateClient("mailgun");
                var variables = JsonSerializer.Deserialize<Dictionary<string, string>>(JsonSerializer.Serialize(templateData));

                var formContent = new FormUrlEncodedContent(new Dictionary<string, string> {
                 { "from", $"{fromName} <{fromEmail}>" },
                 { "to", $"{toName} <{toEmail}>" },
                 { "subject", subject  },
                 { "template", templateId },
                 { "h:Reply-To", $"{fromName} <{replyTo??fromEmail}>" },
                 { "h:X-Mailgun-Variables", JsonSerializer.Serialize(variables.ToDictionary(d => d.Key, d => d.Value)) }
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
                    _logger.LogWarning($"MailgunMessageSender: Failed to log sent email {toEmail}");
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
                        TemplateData = templateData,
                        Cc = cc,
                        UserId = _httpContextAccessor.HttpContext?.User?.Identity?.Name,
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

        public async Task SendEmailAsync(string toEmail, string toName, string htmlMessage, string fromName, string fromEmail, Dictionary<string, string>? customArgs, string subject, string? replyTo = null,
            List<string>? cc = null, List<string>? bcc = null)
        {
            try
            {
                var formContent = new FormUrlEncodedContent(new Dictionary<string, string> {
                 { "from", $"{fromName} <{fromEmail}>" },
                 { "to", $"{toName} <{toEmail}>" },
                 { "subject", subject },
                 { "text", htmlMessage },
                 { "h:X-Mailgun-Variables", JsonSerializer.Serialize(customArgs)}
                 });

                using var httpClient = _httpClientFactory.CreateClient("mailgun");
                var response = await httpClient.PostAsync($"{_emailSettings.Value.ApiBaseUrl}messages", formContent);
                var mailGunResponse = JsonSerializer.Deserialize<MailGunResponseModel>(await response.Content.ReadAsStringAsync(), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                });

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
                        HtmlMessage = htmlMessage,
                        Cc = cc,
                        UserId = _httpContextAccessor.HttpContext?.User?.Identity?.Name,
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

        public async Task SendEmailWithAttachmentAsync(string toEmail, string toName, string templateId, object templateData, string fromEmail, string fromName, EmailAttachmentModel? attachment,
            string subject)
        {
            try
            {
                using var httpClient = _httpClientFactory.CreateClient("mailgun");
                var variables = JsonSerializer.Deserialize<Dictionary<string, string>>(JsonSerializer.Serialize(templateData));

                var multipart = new MultipartFormDataContent
                {
                    { new StringContent($"{fromName} <{fromEmail}>"), "from" },
                    { new StringContent($"{toName} <{toEmail}>"), "to" },
                    { new StringContent(subject), "subject" },
                    { new StringContent(templateId), "template" },
                    { new StringContent($"{fromName} <{fromEmail}>"), "h:Reply-To" },
                    { new StringContent(JsonSerializer.Serialize(variables.ToDictionary(d => d.Key, d => d.Value))), "h:X-Mailgun-Variables" }
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
                    _logger.LogWarning($"MailgunMessageSender: Failed to log sent email {toEmail}");
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
                        TemplateData = templateData,
                        UserId = _httpContextAccessor.HttpContext?.User?.Identity?.Name,
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

        /// <summary>
        /// 
        /// </summary>
        /// <param name="email">The email message</param>
        /// <param name="subject"></param>
        /// <param name="htmlMessage"></param>
        /// <param name="txtMessage"></param>
        /// <returns></returns>
        public async Task SendEmailAsync(string email, string subject, string htmlMessage, string txtMessage, List<string>? cc = null)
        {
            try
            {
                using (var httpClient = _httpClientFactory.CreateClient("mailgun"))
                {
                    var formFields = new Dictionary<string, string> {
                        { "from", $"{_emailSettings.Value.DisplayName} <{_emailSettings.Value.From}>" },
                        { "h:Reply-To", $"{_emailSettings.Value.DisplayName} <{_emailSettings.Value.ReplyTo}>" },
                        { "to", email },
                        { "subject", subject },
                        { "text", txtMessage },
                        { "html", htmlMessage }
                    };

                    if (cc != null && cc.Any())
                        foreach (var c in cc)
                            formFields.Add("cc", c);

                    var formContent = new FormUrlEncodedContent(formFields);

                    var result = await httpClient.PostAsync("messages", formContent);
                    result.EnsureSuccessStatusCode();

                    var mailGunResponse = await result.Content.ReadAsAsync<MailGunResponseModel>();
                    _logger.LogInformation($"Sent message to mailgun - {mailGunResponse.Id} - {mailGunResponse.Message}");

                    var tenantInfo = _httpContextAccessor.HttpContext.GetMultiTenantContext<TenantSetting>()?.TenantInfo;
                    using (var session = _store.OpenAsyncSession($"TenantIdentity-{tenantInfo.Identifier}"))
                    {
                        var newEmail = new SentEmail
                        {
                            Id = $"SentEmails/{mailGunResponse.Id[1..^1]}",
                            From = formFields["from"],
                            ReplyTo = formFields["h:Reply-To"],
                            To = new List<string> { email },
                            Subject = subject,
                            TxtMessage = txtMessage,
                            HtmlMessage = htmlMessage,
                            Cc = cc,
                            UserId = _httpContextAccessor.HttpContext?.User?.Identity?.Name,
                            RefId = mailGunResponse.Id
                        };
                        await session.StoreAsync(newEmail);
                        session.Advanced.GetMetadataFor(newEmail)["@expires"] = DateTime.UtcNow.AddDays(60);
                        await session.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Sending email failed");

                throw;
            }
        }
    }
}
