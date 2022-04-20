using BeyondAuth.Acl;
using System.Text.Json.Serialization;

namespace IdentityManager.Domain
{
    /// <summary>
    /// 
    /// </summary>
    public class Group : ISecurableEntity
    {
        /// <summary>
        /// Group identifier
        /// </summary>
        public string Id => $"Groups/{Name}";

        /// <summary>
        /// Parent
        /// </summary>
        public string? ParentId { get; set; }

        /// <summary>
        /// Owner of group
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

        /// <summary>
        /// Name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Custom tags associated with the group. Can be anything
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// The date when the user was created
        /// </summary>
        public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Last date account was updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        [JsonIgnore]
        public ISecurableEntity? AclHolder { get; set; }

        /// <summary>
        /// Dictionary of UserId / membership metadata
        /// </summary>
        public Dictionary<string, GroupMemberInfo> Members { get; set; } = new Dictionary<string, GroupMemberInfo>();
    }

    public class GroupMemberInfo
    {
        public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;
    }
}
