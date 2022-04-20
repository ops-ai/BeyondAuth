using BeyondAuth.Acl;
using IdentityServer4.Models;
using System.Text.Json.Serialization;

namespace IdentityManager.Domain
{
    /// <summary>
    /// 
    /// </summary>
    public class ApiScopeEntity : ApiScope, ISecurableEntity
    {
        /// <summary>
        /// Document identifier
        /// </summary>
        public string Id => $"Scopes/{Name}";

        /// <summary>
        /// Parent
        /// </summary>
        public string? ParentId { get; set; }

        /// <summary>
        /// Owner of client
        /// </summary>
        public string? OwnerId { get; set; }

        /// <summary>
        /// Id of the nearest parent that contains the ACEs
        /// </summary>
        public string? NearestSecurityHolderId { get; set; }

        /// <summary>
        /// Nesting level of inheritance
        /// </summary>
        public int Level { get; set; } = 0;

        /// <summary>
        /// List of ACEs storing permissions for the secured entity
        /// </summary>
        public List<AceEntry>? AceEntries { get; set; }

        [JsonIgnore]
        public ISecurableEntity? AclHolder { get; set; }
    }
}
