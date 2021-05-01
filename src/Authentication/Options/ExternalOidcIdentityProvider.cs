using Identity.Core;

namespace Authentication.Options
{
    /// <summary>
    /// 
    /// </summary>
    public class ExternalOidcIdentityProvider : IExternalIdentityProvider
    {
        /// <summary>
        /// External client id
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Client secret
        /// </summary>
        public string ClientSecret { get; set; }

        /// <summary>
        /// Protocol
        /// </summary>
        public string Protocol => "oidc";

        /// <summary>
        /// Provider name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Provider is enabled
        /// </summary>
        public bool Enabled { get; set; }
    }
}
