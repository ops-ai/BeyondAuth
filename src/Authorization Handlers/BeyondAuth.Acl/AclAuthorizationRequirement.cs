using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;

namespace BeyondAuth.Acl
{
    [DebuggerDisplay("Name: {Name}, Bit mask: {Bitmask}")]
    //
    // Summary:
    //     A helper class to provide a useful Microsoft.AspNetCore.Authorization.IAuthorizationRequirement
    //     which contains a name.
    public class AclAuthorizationRequirement : IAuthorizationRequirement
    {
        public AclAuthorizationRequirement()
        {

        }

        public AclAuthorizationRequirement(ulong mask)
        {
            Bitmask = mask;
        }

        public AclAuthorizationRequirement(ulong mask, string name)
        {
            Bitmask = mask;
            Name = name;
        }

        /// <summary>
        /// The name of the permission
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Permission bit mask
        /// </summary>
        public ulong Bitmask { get; set; }
    }
}
