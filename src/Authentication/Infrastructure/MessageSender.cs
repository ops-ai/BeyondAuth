using Authentication.Models;
using Authentication.Options;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Threading.Tasks;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
using Twilio.Types;

namespace Authentication.Infrastructure
{
    /// <summary>
    /// This class is used by the application to send Email and SMS
    /// when you turn on two-factor authentication in ASP.NET Identity.
    /// For more details see this link http://go.microsoft.com/fwlink/?LinkID=532713
    /// </summary>
    public class MessageSender : IEmailSender, ISmsSender
    {
        private readonly ILogger _logger;
        private readonly IOptions<SmsOptions> _smsSettings;
        private readonly IOptions<EmailOptions> _emailSettings;
        private readonly IDocumentStore _store;
        private readonly IHttpClientFactory _clientFactory;
        private readonly IHttpContextAccessor _httpContextAccessor;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="loggerFactory"></param>
        /// <param name="smsSettings"></param>
        /// <param name="emailSettings"></param>
        /// <param name="store"></param>
        /// <param name="clientFactory"></param>
        /// <param name="httpContextAccessor"></param>
        public MessageSender(ILoggerFactory loggerFactory, IOptions<SmsOptions> smsSettings, IOptions<EmailOptions> emailSettings, IDocumentStore store, IHttpClientFactory clientFactory, IHttpContextAccessor httpContextAccessor)
        {
            _logger = loggerFactory.CreateLogger<MessageSender>();
            _smsSettings = smsSettings;
            _emailSettings = emailSettings;
            _store = store;
            _clientFactory = clientFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="email">The email message</param>
        /// <param name="subject"></param>
        /// <param name="htmlMessage"></param>
        /// <param name="txtMessage"></param>
        /// <returns></returns>
        public async Task SendEmailAsync(string email, string subject, string htmlMessage, string txtMessage, string cc = null)
        {
            try
            {
                using (var httpClient = _clientFactory.CreateClient("mailgun"))
                {
                    var formFields = new Dictionary<string, string> {
                        { "from", $"{_emailSettings.Value.DisplayName} <{_emailSettings.Value.From}>" },
                        { "h:Reply-To", $"{_emailSettings.Value.DisplayName} <{_emailSettings.Value.ReplyTo}>" },
                        { "to", email },
                        { "subject", subject },
                        { "text", txtMessage },
                        { "html", htmlMessage }
                    };

                    if (cc != null)
                        formFields.Add("cc", cc);

                    var formContent = new FormUrlEncodedContent(formFields);

                    var result = await httpClient.PostAsync("messages", formContent);
                    result.EnsureSuccessStatusCode();

                    var mailGunResponse = await result.Content.ReadAsAsync<MailGunResponseModel>();
                    _logger.LogInformation($"Sent message to mailgun - {mailGunResponse.Id} - {mailGunResponse.Message}");

                    using (var session = _store.OpenAsyncSession())
                    {
                        var newEmail = new SentEmail
                        {
                            Id = $"SentEmails/{mailGunResponse.Id[1..^1]}",
                            From = formFields["from"],
                            ReplyTo = formFields["h:Reply-To"],
                            To = email,
                            Subject = subject,
                            TxtMessage = txtMessage,
                            HtmlMessage = htmlMessage,
                            Cc = cc,
                            UserId = _httpContextAccessor.HttpContext?.User.FindFirst(ClaimTypes.Email)?.Value,
                            RefId = mailGunResponse.Id
                        };
                        await session.StoreAsync(newEmail);
                        session.Advanced.GetMetadataFor(newEmail)["@expires"] = DateTime.UtcNow.AddDays(30);
                        await session.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogCritical("Sending email failed", ex);

                throw;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="number"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task SendSmsAsync(string number, string message)
        {
            try
            {
                var accountSid = _smsSettings.Value.SmsAccountIdentification;
                var authToken = _smsSettings.Value.SmsAccountPassword;

                TwilioClient.Init(accountSid, authToken);

                await MessageResource.CreateAsync(new PhoneNumber(number), from: new PhoneNumber(_smsSettings.Value.SmsAccountFrom), body: message);
            }
            catch (Exception ex)
            {
                _logger.LogCritical("Sending SMS failed", ex);
            }
        }

        private static string GetQueryString(Dictionary<string, string> nvc)
        {
            var array = (from key in nvc.Keys select string.Format("{0}={1}", WebUtility.UrlEncode(key), WebUtility.UrlEncode(nvc[key]))).ToArray();
            return "?" + string.Join("&", array);
        }
    }
}
