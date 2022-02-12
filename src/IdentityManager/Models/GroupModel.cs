using System.ComponentModel.DataAnnotations;

namespace IdentityManager.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class GroupModel
    {
        /// <summary>
        /// Name
        /// </summary>
        [Required]
        public string Name { get; set; }

        /// <summary>
        /// Custom tags associated with the group. Can be anything
        /// </summary>
        public List<string> Tags { get; set; } = new List<string>();

        /// <summary>
        /// The date when the user was created
        /// </summary>
        public DateTime? CreatedOnUtc { get; set; }

        /// <summary>
        /// Last date account was updated
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        public int MemberCount { get; set; }
    }
}
