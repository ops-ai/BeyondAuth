using IdentityServer4.Models;
using System;

namespace IdentityManager.Domain
{
    /// <summary>
    /// 
    /// </summary>
    public class ClientSecretEntity : Secret
    {
        /// <summary>
        /// Document identifier
        /// </summary>
        public string Id => Guid.NewGuid().ToString();
    }
}
