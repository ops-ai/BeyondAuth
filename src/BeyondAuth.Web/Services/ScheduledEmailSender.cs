using BeyondAuth.Web.Models;
using BeyondAuth.Web.Models.Account;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Raven.Client.Documents;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BeyondAuth.Web.Services
{
    public class ScheduledEmailSender : IScheduledEmailSender
    {
        private readonly IDocumentStore _store;
        private readonly ILogger _logger;
        private readonly IEmailSender _emailSender;
        private readonly IServiceProvider _serviceProvider;

        public ScheduledEmailSender(IDocumentStore store, ILoggerFactory loggerFactory, IEmailSender emailSender, IServiceProvider serviceProvider)
        {
            _store = store;
            _logger = loggerFactory.CreateLogger<ScheduledEmailSender>();
            _emailSender = emailSender;
            _serviceProvider = serviceProvider;
        }

        public async Task SendEmailAsync(string id)
        {
            try
            {
                using (var session = _store.OpenAsyncSession())
                {
                    var emailToSend = await session.LoadAsync<ScheduledEmail>(id);

                    var userEmails = new HashSet<string>();

                    switch (emailToSend.List)
                    {
                        case "All Users":
                            foreach (var email in await session.Query<ApplicationUser>().Where(t => !t.Deleted).Select(t => t.Email).Take(int.MaxValue).ToListAsync())
                                userEmails.Add(email);
                            break;
                        case "All Active Users":
                            foreach (var email in await session.Query<ApplicationUser>().Where(t => !t.Deleted && !t.Deactivated).Select(t => t.Email).Take(int.MaxValue).ToListAsync())
                                userEmails.Add(email);
                            break;
                        case "Users with Active Subscription":
                            foreach (var email in await session.Query<ApplicationUser>().Where(t => !t.Deleted && !t.Deactivated && t.UserSubscriptions.Any(s => s.Status == SubscriptionStatus.active && s.ExpirationDate > DateTime.UtcNow)).Select(t => t.Email).Take(int.MaxValue).ToListAsync())
                                userEmails.Add(email);
                            break;
                        case "Users with Active Subscription and no Activity in 30 days":
                            {
                                var oldestLogin = DateTime.UtcNow.AddDays(-30);
                                foreach (var email in await session.Query<ApplicationUser>().Where(t => !t.Deleted && !t.Deactivated && t.UserSubscriptions.Any(s => s.Status == SubscriptionStatus.active && s.ExpirationDate > DateTime.UtcNow) && t.LastLoggedIn < oldestLogin).Select(t => t.Email).Take(int.MaxValue).ToListAsync())
                                    userEmails.Add(email);
                                break;
                            }
                        case "Users with Active Subscription and no Activity in 60 days":
                            {
                                var oldestLogin = DateTime.UtcNow.AddDays(-60);
                                foreach (var email in await session.Query<ApplicationUser>().Where(t => !t.Deleted && !t.Deactivated && t.UserSubscriptions.Any(s => s.Status == SubscriptionStatus.active && s.ExpirationDate > DateTime.UtcNow) && t.LastLoggedIn < oldestLogin).Select(t => t.Email).Take(int.MaxValue).ToListAsync())
                                    userEmails.Add(email);
                                break;
                            }
                        case "Users with Active Subscription and no Activity in 90 days":
                            {
                                var oldestLogin = DateTime.UtcNow.AddDays(-90);
                                var query = session.Query<ApplicationUser>().Where(t => !t.Deleted && !t.Deactivated && t.UserSubscriptions.Any(s => s.Status == SubscriptionStatus.active && s.ExpirationDate > DateTime.UtcNow) && t.LastLoggedIn < oldestLogin);
                                foreach (var email in await query.Select(t => t.Email).Take(int.MaxValue).ToListAsync())
                                    userEmails.Add(email);
                                break;
                            }
                        case "Users with Active Subscription and no Activity in 180 days":
                            {
                                var oldestLogin = DateTime.UtcNow.AddDays(-180);
                                foreach (var email in await session.Query<ApplicationUser>().Where(t => !t.Deleted && !t.Deactivated && t.UserSubscriptions.Any(s => s.Status == SubscriptionStatus.active && s.ExpirationDate > DateTime.UtcNow) && t.LastLoggedIn < oldestLogin).Select(t => t.Email).Take(int.MaxValue).ToListAsync())
                                    userEmails.Add(email);
                                break;
                            }
                    }

                    foreach (var email in emailToSend.IncludeEmails)
                        userEmails.Add(email);

                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var emailService = scope.ServiceProvider.GetService<IEmailService>();
                        foreach (var email in userEmails)
                        {
                            try
                            {
                                var emailMessage = new EmailMessage { To = email, Content = emailToSend.EmailBody };
                                var bodyHtml = await emailService.RenderPartialViewToString("CustomEmail.html", emailMessage, null);
                                var bodyTxt = await emailService.RenderPartialViewToString("CustomEmail.txt", emailMessage, null);

                                await _emailSender.SendEmailAsync(email, emailToSend.Subject, bodyHtml, bodyTxt);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, $"Error sending blast to email {email}");
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending emails");
            }
        }
    }
}
