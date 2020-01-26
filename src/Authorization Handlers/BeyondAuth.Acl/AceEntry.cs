using System.Diagnostics;

namespace BeyondAuth.Acl
{
    [DebuggerDisplay("Subject: {Subject}, Allow: {AllowBits}, Deny: {DenyBits}")]
    public class AceEntry
    {
        /// <summary>
        /// Identifier of the user or group
        /// </summary>
        public string Subject { get; set; }

        /// <summary>
        /// Computed bitwise-map of allowed permissions
        /// </summary>
        public uint AllowBits { get; set; }

        /// <summary>
        /// Computed bitwise-map of denied permissions
        /// </summary>
        public uint DenyBits { get; set; }
    }
}
