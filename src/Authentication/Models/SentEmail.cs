namespace Authentication.Models
{
    /// <summary>
    /// Temporarily saved email message
    /// </summary>
    public class SentEmail
    {
        /// <summary>
        /// Unique id
        /// Pattern: SentEmails/
        /// </summary>
        public string Id { get; set; }

        public string? TemplateId { get; set; }

        public object? TemplateData { get; set; }

        /// <summary>
        /// Email subject
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// Date created
        /// </summary>
        public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Id of the user who sent message
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// To
        /// </summary>
        public List<string> To { get; set; }

        /// <summary>
        /// Cc
        /// </summary>
        public List<string>? Cc { get; set; }

        /// <summary>
        /// Bcc
        /// </summary>
        public List<string>? Bcc { get; set; }

        /// <summary>
        /// From
        /// </summary>
        public string From { get; set; }

        /// <summary>
        /// Reply-To
        /// </summary>
        public string ReplyTo { get; set; }

        /// <summary>
        /// Email text part
        /// </summary>
        public string TxtMessage { get; set; }

        /// <summary>
        /// Email html part
        /// </summary>
        public string HtmlMessage { get; set; }

        /// <summary>
        /// Reference id to sending service
        /// </summary>
        public string RefId { get; set; }

        public List<EmailEvent> Events { get; set; } = new List<EmailEvent>();
    }

    public class MailGunResponseModel
    {
        public string Id { get; set; }

        public string Message { get; set; }
    }

    public class EmailEvent
    {
        public string Id { get; set; }

        public string Name { get; set; }

        public string? Recipient { get; set; }

        public DateTime CreatedOnUtc { get; set; }

        public string? Location { get; set; }

        public string? LogLevel { get; set; }

        public string? Ip { get; set; }
    }
}
