using System.Collections.Generic;

namespace BeyondAuth.Acl
{
    public class SecurableEntity : ISecurableEntity
    {
        /// <summary>
        /// Resource unique identifier
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Resource parent
        /// </summary>
        public string ParentId { get; set; }

        /// <summary>
        /// Resource Owner
        /// </summary>
        public string OwnerId { get; set; }

        /// <summary>
        /// Id of the nearest parent that contains the ACEs
        /// </summary>
        public string NearestSecurityHolderId { get; set; }

        /// <summary>
        /// Nesting level of inheritance
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// List of ACEs storing permissions for the secured entity
        /// </summary>
        public List<AceEntry> AceEntries { get; set; }
    }
}
