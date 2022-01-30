using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IdentityManager.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class GroupMemberAddModel
    {
        /// <summary>
        /// Ids
        /// </summary>
        [Required]
        public List<string> Ids { get; set; }
    }
}
