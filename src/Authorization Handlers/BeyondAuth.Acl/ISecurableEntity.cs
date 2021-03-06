﻿using System.Collections.Generic;

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
        string ParentId { get; }

        /// <summary>
        /// Resource Owner
        /// </summary>
        string OwnerId { get; }

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
