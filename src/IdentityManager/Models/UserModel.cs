using System;
using System.Collections.Generic;

namespace IdentityManager.Models
{
    /// <summary>
    /// 
    /// </summary>
    public class UserModel
    {
        /// <summary>
        /// User ID
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// First Name
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Last Name
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Display Name
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Email Address
        /// </summary>
        public string Email { get; set; }

        /// <summary>
        /// Organization
        /// </summary>
        public string Organization { get; set; }

        /// <summary>
        /// Phone Numbers
        /// </summary>
        public List<string> PhoneNumbers { get; set; }

        /// <summary>
        /// Allow user to reset his own password
        /// </summary>
        public bool PasswordResetAllowed { get; set; }

        /// <summary>
        /// Allow user to change his own password
        /// </summary>
        public bool ChangePasswordAllowed { get; set; }

        /// <summary>
        /// Password policy
        /// </summary>
        public string PasswordPolicy { get; set; }

        /// <summary>
        /// Account is disabled
        /// </summary>
        public bool Disabled { get; set; }

        /// <summary>
        /// Account is locked
        /// </summary>
        public bool Locked { get; set; }

        /// <summary>
        /// The time when the lockout is set to expire
        /// </summary>
        public DateTime? LockoutEnd { get; set; }

        /// <summary>
        /// Date last logged in successfully
        /// </summary>
        public DateTime? LastLoggedIn { get; set; }

        /// <summary>
        /// Date the user's password expires
        /// </summary>
        public DateTime? PasswordExpiry { get; set; }

        /// <summary>
        /// Date the user's account expires
        /// </summary>
        public DateTime? AccountExpiration { get; set; }

        /// <summary>
        /// Claims
        /// </summary>
        public Dictionary<string, string> Claims { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Require the user to change their password the next time they log in
        /// </summary>
        public bool ChangePasswordOnNextLogin { get; set; } = false;

        /// <summary>
        /// Optional one-time access token to auto-login
        /// </summary>
        public string? Otac { get; set; }

        /// <summary>
        /// TimeZone info for current user in IANA format. (e.g. America/Los_Angeles)
        /// </summary>
        public string ZoneInfo { get; set; }

        /// <summary>
        /// User can be locked out if repeatedly enters password incorrect
        /// </summary>
        public bool LockoutEnabled { get; set; }

        /// <summary>
        /// User's default language
        /// </summary>
        public string Locale { get; set; }

        /// <summary>
        /// The date when the user was created
        /// </summary>
        public DateTime? CreatedOnUtc { get; set; }

        /// <summary>
        /// Default redirect upon login unless authenticating into a specific app
        /// </summary>
        public string DefaultApp { get; set; }

        /// <summary>
        /// Last date account was updated
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Email address was confirmed by the user
        /// </summary>
        public bool EmailConfirmed { get; set; }

        /// <summary>
        /// Number of invalid password attempts since last successful authentication
        /// </summary>
        public int AccessFailedCount { get; set; }

        /// <summary>
        /// Two-factor authentication enabled
        /// </summary>
        public bool TwoFactorEnabled { get; set; }

        /// <summary>
        /// Phone number
        /// </summary>
        public string? PhoneNumber { get; set; }

        /// <summary>
        /// Phone number has been confirmed by the user
        /// </summary>
        public bool PhoneNumberConfirmed { get; set; }

        public string? SecurityStamp { get; set; }
    }
}
