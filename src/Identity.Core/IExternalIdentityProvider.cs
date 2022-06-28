namespace Identity.Core
{
    /// <summary>
    /// 
    /// </summary>
    public interface IExternalIdentityProvider
    {
        /// <summary>
        /// Protocol
        /// </summary>
        public string Protocol => "oidc";

        /// <summary>
        /// Authentication Scheme used
        /// </summary>
        public string Scheme { get; set; }

        /// <summary>
        /// Provider name
        /// </summary>
        public string Name { get; set; }

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
