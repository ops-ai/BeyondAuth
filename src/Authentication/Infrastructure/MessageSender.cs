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
using System.Net;
using System.Net.Http;
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
    public class MessageSender : ISmsSender
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
        /// <param name="number"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public async Task<SmsSendStatus> SendSmsAsync(string number, string message)
        {
            try
            {
                var accountSid = _smsSettings.Value.SmsAccountIdentification;
                var authToken = _smsSettings.Value.SmsAccountPassword;

                TwilioClient.Init(accountSid, authToken);

                var response = await MessageResource.CreateAsync(new PhoneNumber(number), from: new PhoneNumber(_smsSettings.Value.SmsAccountFrom), body: message);

                var tenantInfo = _httpContextAccessor.HttpContext.GetMultiTenantContext<TenantSetting>()?.TenantInfo;
                using (var session = _store.OpenAsyncSession($"TenantIdentity-{tenantInfo.Identifier}"))
                {
                    var newSms = new SentSms
                    {
                        Id = $"SentSms/{response.Sid}",
                        From = response.From.ToString(),
                        To = response.To,
                        Message = message,
                        Events = new List<SmsEvent> { new SmsEvent { CreatedOnUtc = DateTime.UtcNow, Id = "", Name = response.Status.ToString() } }
                    };
                    await session.StoreAsync(newSms);
                    session.Advanced.GetMetadataFor(newSms)["@expires"] = DateTime.UtcNow.AddMonths(2);
                    await session.SaveChangesAsync();
                }

                if (response.Status == MessageResource.StatusEnum.Accepted)
                    return SmsSendStatus.Successful;
                else if (response.Status == MessageResource.StatusEnum.Failed)
                    return SmsSendStatus.Blocked;
                else
                    return SmsSendStatus.Failed;
            }
            catch (Exception ex)
            {
                _logger.LogCritical(ex, "Sending SMS failed");

                return SmsSendStatus.Failed;
            }
        }

        /// <summary>
        /// SMS send status
        /// </summary>
        public enum SmsSendStatus
        {
            /// <summary>
            /// Message was accepted for delivery
            /// </summary>
            Successful,

            /// <summary>
            /// Message queueing failed
            /// </summary>
            Failed,

            /// <summary>
            /// Sending was blocked
            /// </summary>
            Blocked
        }

        private static string GetQueryString(Dictionary<string, string> nvc)
        {
            var array = (from key in nvc.Keys select string.Format("{0}={1}", WebUtility.UrlEncode(key), WebUtility.UrlEncode(nvc[key]))).ToArray();
            return "?" + string.Join("&", array);
        }
    }
}
