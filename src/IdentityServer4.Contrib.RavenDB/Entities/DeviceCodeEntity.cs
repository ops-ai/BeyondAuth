using IdentityServer4.Stores.Serialization;
using System;
using System.Collections.Generic;

namespace IdentityServer4.Contrib.RavenDB.Entities
{
    public class DeviceCodeEntity
    {
        /// <summary>
        /// User code
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Device code
        /// </summary>
        public string DeviceCode { get; set; }

        /// <summary>
        /// Gets or sets the creation time.
        /// </summary>
        public DateTime CreationTime { get; set; }

        /// <summary>
        /// Gets or sets the lifetime.
        /// </summary>
        public int Lifetime { get; set; }

        /// <summary>
        /// Gets or sets the client identifier.
        /// </summary>
        public string ClientId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is open identifier.
        /// </summary>
        public bool IsOpenId { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is authorized.
        /// </summary>
        public bool IsAuthorized { get; set; }

        /// <summary>
        /// Gets or sets the requested scopes.
        /// </summary>
        public IEnumerable<string> RequestedScopes { get; set; }

        /// <summary>
        /// Gets or sets the authorized scopes.
        /// </summary>
        public IEnumerable<string> AuthorizedScopes { get; set; }

        /// <summary>
        /// List of claims associated with principal
        /// </summary>
        public ClaimsPrincipalLite Principal { get; set; }
    }
}
