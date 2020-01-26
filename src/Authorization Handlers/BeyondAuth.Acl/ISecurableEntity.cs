using System;

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
    }
}
