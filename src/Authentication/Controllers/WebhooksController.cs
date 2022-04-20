using Authentication.Filters;
using Authentication.Models;
using Identity.Core.Settings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Raven.Client.Documents;
using System.Security.Cryptography;
using System.Text;

namespace Authentication.Controllers
{
    [SecurityHeaders]
    [AllowAnonymous]
    public class WebhooksController : Controller
    {
        private readonly IDocumentStore _store;
        private readonly ILogger _logger;
        private readonly IOptions<EmailOptions> _emailSettings;

        public WebhooksController(IDocumentStore store, ILoggerFactory loggerFactory, IOptions<EmailOptions> emailSettings)
        {
            _store = store;
            _logger = loggerFactory.CreateLogger<WebhooksController>();
            _emailSettings = emailSettings;
        }

        [AllowAnonymous]
        [HttpPost("mailgun")]
        public async Task<IActionResult> EmailStatusFromMailgun([FromBody] MailgunWebhookModel emailEvent, CancellationToken ct = default)
        {
            var digest = ComputeSignature(emailEvent.Signature.Timestamp + emailEvent.Signature.Token);
            if (!digest.Equals(emailEvent.Signature.Signature, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogWarning("Mailgun signature validation failed");
                throw new InvalidOperationException("Invalid signature");
            }

            using var session = _store.OpenAsyncSession();

            var id = emailEvent.EventData.Message.Headers.MessageId.Split('@').FirstOrDefault();

            var email = await session.LoadAsync<SentEmail>($"SentEmails/{id}", ct);
            if (email == null)
                throw new KeyNotFoundException($"Email {id} not found");

            email.Events.Add(new EmailEvent
            {
                CreatedOnUtc = DateTime.UtcNow,
                Recipient = emailEvent.EventData.Recipient,
                Id = emailEvent.EventData.Id,
                LogLevel = emailEvent.EventData.LogLevel,
                Name = emailEvent.EventData.Event,
                Ip = emailEvent.EventData.Ip
            });
            await session.SaveChangesAsync(ct);

            return Ok();
        }

        private string ComputeSignature(string stringToSign)
        {
            var secret = _emailSettings.Value.WebhookSigningKey;
            using (var hmac = new HMACSHA256(Encoding.ASCII.GetBytes(secret)))
            {
                var signature = hmac.ComputeHash(Encoding.ASCII.GetBytes(stringToSign));
                return BitConverter.ToString(signature).Replace("-", "");
            }
        }

        [AllowAnonymous]
        [HttpPost("sms")]
        public async Task<IActionResult> UpdateSmsStatusCallback([FromForm] SmsWebhookModel model, CancellationToken ct = default)
        {
            using var session = _store.OpenAsyncSession();
            var smsEvent = new SmsEvent { CreatedOnUtc = DateTime.UtcNow, Id = model.SmsSid, Name = model.MessageStatus };
            session.Advanced.Patch<SentSms, SmsEvent>($"SentSms/{model.MessageSid}", t => t.Events, t => t.Add(smsEvent));

            await session.SaveChangesAsync(ct);

            return Ok();
        }
    }
}
