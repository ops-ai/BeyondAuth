using System;
using System.Collections.Generic;

namespace Authentication.Models
{
    /// <summary>
    /// Temporarily saved SMS message
    /// </summary>
    public class SentSms
    {
        /// <summary>
        /// Unique id
        /// Pattern: SentSms/
        /// </summary>
        public string Id { get; set; }

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
        /// From
        /// </summary>
        public string From { get; set; }

        /// <summary>
        /// Email text part
        /// </summary>
        public string Message { get; set; }

        /// <summary>
        /// Delivery events
        /// </summary>
        public List<SmsEvent> Events { get; set; } = new List<SmsEvent>();
    }

    /// <summary>
    /// Message delivery event
    /// </summary>
    public class SmsEvent
    {
        /// <summary>
        /// Unique event identifier
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Event name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Event timestamp
        /// </summary>
        public DateTime CreatedOnUtc { get; set; }
    }
}
