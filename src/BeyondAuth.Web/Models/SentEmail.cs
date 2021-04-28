using System;
using System.Collections.Generic;

namespace BeyondAuth.Web.Models
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
        public string UserId { get; set; }

        /// <summary>
        /// To
        /// </summary>
        public string To { get; set; }

        /// <summary>
        /// Cc
        /// </summary>
        public string Cc { get; set; }

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

        /// <summary>
        /// Email deliverability events
        /// </summary>
        public List<EmailEvent> Events { get; set; } = new List<EmailEvent>();
    }

    /// <summary>
    /// Mailgun response mapping
    /// </summary>
    public class MailGunResponseModel
    {
        /// <summary>
        /// Message id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Response status message
        /// </summary>
        public string Message { get; set; }
    }

    /// <summary>
    /// Email deliverability event
    /// </summary>
    public class EmailEvent
    {
        /// <summary>
        /// Unique mailgun id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Event name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Date event ocurred
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }
    }
}
