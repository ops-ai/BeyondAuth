using Microsoft.AspNetCore.Authorization;
using System.Diagnostics;

namespace IdentityManager.Extensions
{
    [DebuggerDisplay("Bit mask: {Bitmask}")]
    //
    // Summary:
    //     A helper class to provide a useful Microsoft.AspNetCore.Authorization.IAuthorizationRequirement
    //     which contains a name.
    public class TenantAuthorizationRequirement : IAuthorizationRequirement
    {
        public TenantAuthorizationRequirement()
        {

        }

        public TenantAuthorizationRequirement(ulong mask)
        {
            Bitmask = mask;
        }

        /// <summary>
        /// Permission bit mask
        /// </summary>
        public ulong Bitmask { get; set; }
    }
}
