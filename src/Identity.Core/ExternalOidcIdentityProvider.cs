namespace Identity.Core
{
    /// <summary>
    /// 
    /// </summary>
    public class ExternalOidcIdentityProvider : IExternalIdentityProvider
    {
        public string Authority { get; set; }

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
        /// Authentication Scheme used
        /// </summary>
        public string Scheme { get; set; }

        /// <summary>
        /// Provider is enabled
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// Icon to display for the custom provider
        /// </summary>
        public string? Icon { get; set; }

        /// <summary>
        /// Icon/button color
        /// </summary>
        public string? Color { get; set; }
    }
}
