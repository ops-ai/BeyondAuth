using System.Diagnostics;

namespace BeyondAuth.Acl
{
    [DebuggerDisplay("IdP: {IdP}, Subject: {Subject}, Allow: {AllowBits}, Deny: {DenyBits}")]
    public class AceEntry
    {
        /// <summary>
        /// Optional Identity Provider the user primarily belongs to
        /// </summary>
        public string? IdP { get; set; }

        /// <summary>
        /// Identifier of the user or group
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// Computed bitwise-map of allowed permissions
        /// </summary>
        public ulong AllowBits { get; set; } = 0;

        /// <summary>
        /// Computed bitwise-map of denied permissions
        /// </summary>
        public ulong DenyBits { get; set; } = 0;
    }
}
