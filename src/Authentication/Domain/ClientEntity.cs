using IdentityServer4.Models;

namespace Authentication.Domain
{
    /// <summary>
    /// 
    /// </summary>
    public class ClientEntity : Client
    {
        /// <summary>
        /// Document identifier
        /// </summary>
        public string Id => $"Clinets/{ClientId}";

        /// <summary>
        /// Customer support email that should be displayed
        /// </summary>
        public string SupportEmail { get; set; }

        /// <summary>
        /// Customer support link that should be displayed
        /// </summary>
        public string SupportLink { get; set; }
    }
}
