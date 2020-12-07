using Identity.Core;
using Identity.Core.Extensions;
using IdentityModel;
using IdentityServer.LdapExtension.UserModel;
using Novell.Directory.Ldap;
using System;
using System.Collections.Generic;
using System.Security.Claims;

namespace Authentication.Models
{
    /// <summary>
    /// Base model for email message models
    /// </summary>
    public class EmailMessageModel
    {
        /// <summary>
        /// Email receipient
        /// </summary>
        public string To { get; set; }

        /// <summary>
        /// User's first name
        /// </summary>
        public string FirstName { get; set; }
    }
}
