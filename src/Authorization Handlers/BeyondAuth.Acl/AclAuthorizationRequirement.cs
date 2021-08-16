using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;

namespace BeyondAuth.Acl
{
    [DebuggerDisplay("Name: {Subject}, Bit mask: {Bitmask}")]
    //
    // Summary:
    //     A helper class to provide a useful Microsoft.AspNetCore.Authorization.IAuthorizationRequirement
    //     which contains a name.
    public class AclAuthorizationRequirement : IAuthorizationRequirement
    {
        /// <summary>
        /// The name of the permission
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Permission bit mask
        /// </summary>
        public uint Bitmask { get; set; }
    }
}
