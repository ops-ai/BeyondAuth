﻿using System.Collections.Generic;

namespace Identity.Core.Settings
{
    /// <summary>
    /// Email sending options
    /// </summary>
    public class EmailOptions
    {
        /// <summary>
        /// From field in email
        /// </summary>
        public string From { get; set; }

        /// <summary>
        /// Reply-To header
        /// </summary>
        public string ReplyTo { get; set; }

        /// <summary>
        /// Sender display name
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Email address for support
        /// </summary>
        public string SupportEmail { get; set; }

        /// <summary>
        /// Mailgun sending key
        /// </summary>
        public string SendingKey { get; set; }

        /// <summary>
        /// Mailgun private key
        /// </summary>
        public string PrivateKey { get; set; }

        /// <summary>
        /// HTTP webhook signing key
        /// </summary>
        public string WebhookSigningKey { get; set; }

        /// <summary>
        /// Mailgun Api base url including domain
        /// </summary>
        public string ApiBaseUrl { get; set; }

        /// <summary>
        /// Mailgun Api endpoint ofr message sending
        /// </summary>
        public string? ApiEndpoint { get; set; }

        public Dictionary<string, string> Templates { get; set; } = new Dictionary<string, string>();
    }
}
