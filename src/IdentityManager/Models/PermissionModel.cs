using System.ComponentModel.DataAnnotations;

namespace IdentityManager.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class PermissionModel
    {
        /// <summary>
        /// User ID
        /// </summary>
        [Required]
        public string UserId { get; set; }

        /// <summary>
        /// First Name
        /// </summary>
        [Required]
        public ulong AllowBits { get; set; } = 0;

        /// <summary>
        /// Last Name
        /// </summary>
        [Required]
        public ulong DenyBits { get; set; } = 0;

        /// <summary>
        /// Display Name
        /// </summary>
        public string? IdP { get; set; }
    }
}
