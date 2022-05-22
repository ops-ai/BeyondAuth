using Authentication.Models;

namespace Authentication.Infrastructure
{
    /// <summary>
    /// 
    /// </summary>
    public interface IEmailSender
    {
        Task SendEmailAsync(string toEmail, string toName, string templateId, IEnumerable<TemplateVariable> templateData, string fromName, string fromEmail,
            string subject, string? replyTo = null, List<string>? cc = null, List<string>? bcc = null);

        Task SendEmailWithAttachmentAsync(string toEmail, string toName, string templateId, IEnumerable<TemplateVariable> templateData, string fromEmail, string fromName,
            EmailAttachmentModel attachment, string subject);
    }
}
