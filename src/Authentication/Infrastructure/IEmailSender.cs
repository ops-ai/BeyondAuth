using Authentication.Models;

namespace Authentication.Infrastructure
{
    /// <summary>
    /// 
    /// </summary>
    public interface IEmailSender
    {
        Task SendEmailAsync(string to, string subject, string htmlMessage, string txtMessage, List<string>? cc = null);

        Task SendEmailAsync(string toEmail, string toName, string htmlMessage, string fromName, string fromEmail, Dictionary<string, string>? customArgs,
            string subject, string? replyTo = null, List<string>? cc = null, List<string>? bcc = null);

        Task SendEmailAsync(string toEmail, string toName, string templateId, object templateData, string fromName, string fromEmail,
            string subject, string? replyTo = null, List<string>? cc = null, List<string>? bcc = null);

        Task SendEmailWithAttachmentAsync(string toEmail, string toName, string templateId, object templateData, string fromEmail, string fromName,
            EmailAttachmentModel attachment, string subject);

        Task ReportIssueAsync(string subject, string txtMessage);
    }
}
