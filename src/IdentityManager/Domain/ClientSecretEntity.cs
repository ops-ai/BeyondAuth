using BeyondAuth.Acl;
using IdentityServer4.Models;
using System;
using System.Collections.Generic;

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
