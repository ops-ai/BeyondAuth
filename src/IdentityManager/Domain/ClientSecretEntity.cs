using IdentityServer4.Models;

namespace IdentityManager.Domain
{
    /// <summary>
    /// 
    /// </summary>
    public class ClientSecretEntity : Secret
    {
        /// <summary>
        /// Document identifier
        /// Format: ClientSecrets/{clientId}/{id:guid}
        /// </summary>
        public string Id { get; set; }
    }
}
