using Identity.Core.Extensions;
using IdentityModel;
using IdentityServer.LdapExtension.UserModel;
using Novell.Directory.Ldap;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Identity.Core
{
    public class ApplicationUser : IAppUser
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
        public bool? ChangePasswordOnNextLogin { get; set; } = false;

        /// <summary>
        /// Password policy
        /// </summary>
        public string PasswordPolicy { get; set; } = null;

        /// <summary>
        /// Account expiration date
        /// </summary>
        public DateTime? AccountExpiration { get; set; } = DateTime.MinValue;

        /// <summary>
        /// Account is disabled
        /// </summary>
        public bool Disabled { get; set; } = false;

        /// <summary>
        /// TimeZone info for current user in IANA format. (e.g. America/Los_Angeles)
        /// </summary>
        public string ZoneInfo { get; set; }

        /// <summary>
        /// Primary user identifier
        /// </summary>
        public string SubjectId { get; set; }

        /// <summary>
        /// Directory usernaem
        /// </summary>
        public string Username { get; set; }

        /// <summary>
        /// Subject Id in external provider
        /// </summary>
        public string ProviderSubjectId { get; set; }

        /// <summary>
        /// Provider name
        /// </summary>
        public string ProviderName { get; set; }

        /// <summary>
        /// User is active
        /// </summary>
        public bool IsActive { get; set; }

        /// <summary>
        /// Custom claims stored about user
        /// </summary>
        public ICollection<Claim> Claims { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string[] LdapAttributes => Enum<ActiveDirectoryLdapAttributes>.Descriptions;


        /// <summary>
        /// Fills the claims.
        /// </summary>
        /// <param name="user">The user.</param>
        public void FillClaims(LdapEntry ldapEntry)
        {
            // Example in LDAP we have display name as displayName (normal field)
            Claims = new List<Claim>
                {
                    GetClaimFromLdapAttributes(ldapEntry, JwtClaimTypes.Name, OpenLdapAttributes.DisplayName),
                    GetClaimFromLdapAttributes(ldapEntry, JwtClaimTypes.FamilyName, OpenLdapAttributes.LastName),
                    GetClaimFromLdapAttributes(ldapEntry, JwtClaimTypes.GivenName, OpenLdapAttributes.FirstName),
                    GetClaimFromLdapAttributes(ldapEntry, JwtClaimTypes.Email, OpenLdapAttributes.EMail),
                    GetClaimFromLdapAttributes(ldapEntry, JwtClaimTypes.PhoneNumber, OpenLdapAttributes.TelephoneNumber)
                };

            // Add claims based on the user groups
            // add the groups as claims -- be careful if the number of groups is too large
            if (true)
            {
                try
                {
                    var userRoles = ldapEntry.GetAttribute(OpenLdapAttributes.MemberOf.ToDescriptionString()).StringValues;
                    while (userRoles.MoveNext())
                    {
                        Claims.Add(new Claim(JwtClaimTypes.Role, userRoles.Current.ToString()));
                    }
                    //var roles = userRoles.Current (x => new Claim(JwtClaimTypes.Role, x.Value));
                    //id.AddClaims(roles);
                    //Claims = this.Claims.Concat(new List<Claim>()).ToList();
                }
                catch (Exception)
                {
                    // No roles exists it seems.
                }
            }
        }

        private Claim GetClaimFromLdapAttributes(LdapEntry user, string claim, OpenLdapAttributes ldapAttribute)
        {
            var value = string.Empty;

            try
            {
                value = user.GetAttribute(ldapAttribute.ToDescriptionString()).StringValue;
                return new Claim(claim, value);
            }
            catch (Exception)
            {
                // Should do something... But basically the attribute is not found
                // We swallow for now, since we might not care.
            }

            return new Claim(claim, value);
        }


        /// <summary>
        /// This will set the base details such as:
        /// - DisplayName
        /// - Username
        /// - ProviderName
        /// - SubjectId
        /// - ProviderSubjectId
        /// - Fill the claims
        /// </summary>
        /// <param name="ldapEntry">Ldap Entry</param>
        /// <param name="providerName">Specific provider such as Google, Facebook, etc.</param>
        public void SetBaseDetails(LdapEntry ldapEntry, string providerName)
        {
            DisplayName = ldapEntry.GetAttribute(OpenLdapAttributes.DisplayName.ToDescriptionString()).StringValue;
            Username = ldapEntry.GetAttribute(OpenLdapAttributes.UserName.ToDescriptionString()).StringValue;
            ProviderName = providerName;
            SubjectId = Username; // Extra: We could use the uidNumber instead in a sha algo.
            ProviderSubjectId = Username;
            FillClaims(ldapEntry);
        }
    }
}
