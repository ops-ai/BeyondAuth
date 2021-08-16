using BeyondAuth.Acl;
using Finbuckle.MultiTenant;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Identity.Core
{
    public class TenantSetting : ITenantInfo, ISecurableEntity
    {
        /// <summary>
        /// A unique id for a tenant in the app and should never change
        /// TenantSetting/{host url}
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Account behavior
        /// </summary>
        public AccountOptions AccountOptions { get; set; }

        /// <summary>
        /// Consent behavior
        /// </summary>
        public ConsentOptions ConsentOptions { get; set; }

        /// <summary>
        /// Identity requirements
        /// </summary>
        public IdentityOptions IdentityOptions { get; set; }

        /// <summary>
        /// The value used to actually resolve a tenant and should have a syntax compatible for the app (i.e. no crazy symbols in a web app where the identifier will be part of the URL). 
        /// </summary>
        public string Identifier { get; set; }

        /// <summary>
        /// Display name for the tenant
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Connection string that should be used for database operations for this tenant. It might connect to a shared database or a dedicated database for the single tenant. 
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// Branding options
        /// </summary>
        public BrandingOptions BrandingOptions { get; set; }

        /// <summary>
        /// External login providers
        /// </summary>
        public IList<IExternalIdentityProvider> ExternalIdps = new List<IExternalIdentityProvider>();

        /// <summary>
        /// Parent
        /// </summary>
        public string ParentId { get; set; }

        /// <summary>
        /// Owner of client
        /// </summary>
        public string OwnerId { get; set; }

        /// <summary>
        /// Id of the nearest parent that contains the ACEs
        /// </summary>
        public string NearestSecurityHolderId { get; set; }

        /// <summary>
        /// Nesting level of inheritance
        /// </summary>
        public int Level { get; set; } = 0;

        /// <summary>
        /// List of ACEs storing permissions for the secured entity
        /// </summary>
        public List<AceEntry> AceEntries { get; set; }

        [JsonIgnore]
        public ISecurableEntity AclHolder { get; set; }
    }
}