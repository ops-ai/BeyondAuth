using IdentityModel;
using Newtonsoft.Json;
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
        /// Date the user was last updated
        /// </summary>
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// TimeZone info for current user in IANA format. (e.g. America/Los_Angeles)
        /// </summary>
        public string ZoneInfo { get; set; }

        /// <summary>
        /// User's locale
        /// </summary>
        public string Locale { get; set; }

        public string DefaultApp { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public override List<IdentityUserClaim> Claims
        {
            get
            {
                var claims = new List<IdentityUserClaim>()
                {
                    new IdentityUserClaim { ClaimType = JwtClaimTypes.Subject, ClaimValue = Email },
                    new IdentityUserClaim { ClaimType = JwtClaimTypes.FamilyName, ClaimValue = LastName ?? "" },
                    new IdentityUserClaim { ClaimType = JwtClaimTypes.GivenName, ClaimValue = FirstName ?? "" },
                    new IdentityUserClaim { ClaimType = JwtClaimTypes.Name, ClaimValue = DisplayName ?? "" },
                    new IdentityUserClaim { ClaimType = JwtClaimTypes.Email, ClaimValue = Email },
                    new IdentityUserClaim { ClaimType = JwtClaimTypes.PhoneNumber, ClaimValue = PhoneNumber ?? "" },
                    new IdentityUserClaim { ClaimType = JwtClaimTypes.Locale, ClaimValue = Locale ?? "" },
                    new IdentityUserClaim { ClaimType = JwtClaimTypes.UpdatedAt, ClaimValue = JsonConvert.SerializeObject(UpdatedAt, new JsonSerializerSettings { DateFormatString = "yyyy-MM-ddTHH:mm:ss.fffZ" }) },
                    new IdentityUserClaim { ClaimType = JwtClaimTypes.ZoneInfo, ClaimValue = ZoneInfo ?? "" }
                };

                foreach (var role in Roles)
                    claims.Add(new IdentityUserClaim { ClaimType = JwtClaimTypes.Role, ClaimValue = role });

                return claims;
            }
        }
    }
}
