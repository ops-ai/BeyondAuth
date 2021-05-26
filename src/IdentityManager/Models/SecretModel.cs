using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.ComponentModel.DataAnnotations;
using static IdentityServer4.IdentityServerConstants;

namespace IdentityManager.Models
{
    /// <summary>
    /// Client secret configuration
    /// </summary>
    public class SecretModel
    {
        /// <summary>
        /// Hash of the hash of the secret
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Type of secret. SecretTypes.SharedSecret or SecretTypes.X509CertificateBase64
        /// </summary>
        public SecretTypes Type { get; set; } = SecretTypes.SharedSecret;

        /// <summary>
        /// Description
        /// </summary>
        [Required]
        public string Description { get; set; }

        /// <summary>
        /// Lifetime of identity token in seconds (defaults to 900 seconds / 15 minutes)
        /// </summary>
        public DateTime? Expiration { get; set; }
    }
}
