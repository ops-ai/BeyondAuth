using System.Collections.Generic;

namespace BeyondAuth.Acl
{
    public interface ISecurableEntity
    {
        /// <summary>
        /// Resource unique identifier
        /// </summary>
        string Id { get; }

        /// <summary>
        /// Resource parent
        /// </summary>
        string? ParentId { get; }

        /// <summary>
        /// Resource Owner
        /// </summary>
        string? OwnerId { get; }

        /// <summary>
        /// IdP the resource owner is in
        /// </summary>
        string? OwnerIdP { get; }

        /// <summary>
        /// Id of the nearest parent that contains the ACEs
        /// </summary>
        public string NearestSecurityHolderId { get; }

        /// <summary>
        /// Nesting level of inheritance
        /// </summary>
        public ushort Level { get; }

        /// <summary>
        /// List of ACEs storing permissions for the secured entity
        /// </summary>
        public List<AceEntry>? AceEntries { get; }

        /// <summary>
        /// Reference to Acess control list holder referenced by the NearestSecurityHolderId
        /// </summary>
        public ISecurableEntity? AclHolder { get; } //TODO: This can be virtual on implementation
    }
}
