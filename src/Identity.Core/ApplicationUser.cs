using IdentityModel;
using Raven.Identity;
using System;
using System.Collections.Generic;

namespace Identity.Core
{
    /// <summary>
    /// Application user
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        /// <summary>
        /// First Name
        /// </summary>
        public string FirstName { get; set; }

        /// <summary>
        /// Last Name
        /// </summary>
        public string LastName { get; set; }

        /// <summary>
        /// Date created
        /// </summary>
        public DateTime CreatedOnUtc { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date last logged in
        /// </summary>
        public DateTime? LastLoggedIn { get; set; }

        /// <summary>
        /// Display Name
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Phone numbers
        /// </summary>
        public List<string> PhoneNumbers { get; set; }

        /// <summary>
        /// Organization
        /// </summary>
        public string Organization { get; set; }

        /// <summary>
        /// Allow user to reset his own password
        /// </summary>
        public bool PasswordResetAllowed { get; set; } = true;

        /// <summary>
        /// Allow user to reset his own password
        /// </summary>
        public bool ChangePasswordAllowed { get; set; } = true;

        /// <summary>
        /// Require the user to change their password the next time they log in
        /// </summary>
        public bool ChangePasswordOnNextLogin { get; set; } = false;

        /// <summary>
        /// Password policy
        /// </summary>
        public string PasswordPolicy { get; set; } = null;

        /// <summary>
        /// Account expiration date
        /// </summary>
        public DateTime? AccountExpiration { get; set; }

        /// <summary>
        /// Account is disabled
        /// </summary>
        public bool Disabled { get; set; } = false;

        /// <summary>
        /// TimeZone info for current user in IANA format. (e.g. America/Los_Angeles)
        /// </summary>
        public string ZoneInfo { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public override List<IdentityUserClaim> Claims => new List<IdentityUserClaim>()
        {
            new IdentityUserClaim { ClaimType = JwtClaimTypes.Subject, ClaimValue = Email },
            new IdentityUserClaim { ClaimType = JwtClaimTypes.Name, ClaimValue = DisplayName }
        };

    }
}
