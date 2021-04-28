using System;
using System.Collections.Generic;

namespace BeyondAuth.Web.Models
{
    /// <summary>
    /// Temporarily saved email message
    /// </summary>
    public class ScheduledEmail
    {
        /// <summary>
        /// Unique id
        /// Pattern: ScheduledEmails/
        /// </summary>
        public string Id { get; set; }

        public string Subject { get; set; }

        public string EmailBody { get; set; }

        public string List { get; set; }

        public List<string> IncludeEmails { get; set; }

        public DateTime DateToSend { get; set; } = DateTime.UtcNow;

        public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;
    }
}
