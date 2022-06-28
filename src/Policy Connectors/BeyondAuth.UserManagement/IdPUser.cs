using System;
using System.Collections.Generic;

namespace BeyondAuth.UserManagement
{
    public class IdPUser
    {
        public string Id { get; set; }

        public string Email { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string DisplayName { get; set; }

        public string Organization { get; set; }

        public List<string>? PhoneNumbers { get; set; }

        public bool PasswordResetAllowed { get; set; }

        /// <summary>
        /// Used for setting/updating password. Password can never be retrieved
        /// </summary>
        public string? Password { get; set; }

        public bool ChangePasswordAllowed { get; set; }

        public string? PasswordPolicy { get; set; }

        public bool Disabled { get; set; }

        public bool Locked { get; set; }

        public DateTime? LockoutEnd { get; set; }

        public DateTime? LastLoggedIn { get; set; }

        public DateTime? PasswordExpiry { get; set; }

        public DateTime? AccountExpiration { get; set; }

        public bool ChangePasswordOnNextLogin { get; set; }

        public string? Otac { get; set; }

        public string? ZoneInfo { get; set; }

        /// <summary>
        /// Two-factor authentication enabled
        /// </summary>
        public bool TwoFactorEnabled { get; set; }

        /// <summary>
        /// Email address was confirmed by the user
        /// </summary>
        public bool EmailConfirmed { get; set; }

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

    public class IdPUserCreateRequest : IdPUser
    {
        public bool GenerateOtac { get; set; } = false;
    }
}
