using System;
using static IdentityServer4.IdentityServerConstants;

namespace IdentityManager.Models
{
    /// <summary>
    /// Client secret configuration
    /// </summary>
    public class ClientSecretModel
    {
        /// <summary>
        /// Unique ID representing the secret
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Type of secret. SecretTypes.SharedSecret or SecretTypes.X509CertificateBase64
        /// </summary>
        public string Type { get; set; } = SecretTypes.SharedSecret;

        /// <summary>
        /// Description
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Lifetime of identity token in seconds (defaults to 900 seconds / 15 minutes)
        /// </summary>
        public DateTime? Expiration { get; set; }
    }
}
