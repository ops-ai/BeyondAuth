using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace IdentityManager.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class UserUpdateModel
    {
        /// <summary>
        /// First Name
        /// </summary>
        [Required]
        public string FirstName { get; set; }

        /// <summary>
        /// Last Name
        /// </summary>
        [Required]
        public string LastName { get; set; }

        /// <summary>
        /// Display Name
        /// </summary>
        [Required]
        public string DisplayName { get; set; }

        /// <summary>
        /// Email Address
        /// </summary>
        [Required]
        [EmailAddress(ErrorMessage = "Not a valid email")]
        public string Email { get; set; }

        /// <summary>
        /// Password
        /// </summary>
        public string? Password { get; set; }

        /// <summary>
        /// Organization
        /// </summary>
        [Required]
        public string Organization { get; set; }

        /// <summary>
        /// Allow user to reset his own password
        /// </summary>
        public bool? PasswordResetAllowed { get; set; }

        /// <summary>
        /// Allow user to reset his own password
        /// </summary>
        public bool? ChangePasswordAllowed { get; set; }

        /// <summary>
        /// Require the user to change their password the next time they log in
        /// </summary>
        public bool? ChangePasswordOnNextLogin { get; set; }

        /// <summary>
        /// Password policy
        /// </summary>
        public string? PasswordPolicy { get; set; }

        /// <summary>
        /// Account expiration date
        /// </summary>
        public DateTime? AccountExpiration { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Account is disabled
        /// </summary>
        public bool? Disabled { get; set; }

        /// <summary>
        /// Claims
        /// </summary>
        public Dictionary<string, string> Claims { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// TimeZone info for current user in IANA format. (e.g. America/Los_Angeles)
        /// </summary>
        public string? ZoneInfo { get; set; }

        /// <summary>
        /// User is locked
        /// </summary>
        public bool? Locked { get; set; }

        /// <summary>
        /// User can be locked out
        /// </summary>
        public bool? LockoutEnabled { get; set; }

        /// <summary>
        /// Email has been confirmed by user
        /// </summary>
        public bool? EmailConfirmed { get; set; }

        /// <summary>
        /// Phone number has been confirmed by user
        /// </summary>
        public bool? PhoneNumberConfirmed { get; set; }

        /// <summary>
        /// Phone number
        /// </summary>
        public string? PhoneNumber { get; set; }
    }
}
