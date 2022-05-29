using Audit.Core;

namespace IdentityManager.Data.Audit
{
    public class ScopeEvent : AuditEvent
    {
        /// <summary>
        /// User Id making the change
        /// </summary>
        public string? UserId { get; set; }

        /// <summary>
        /// User agent
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// IP Address
        /// </summary>
        public string? RemoteIpAddress { get; set; }

        /// <summary>
        /// Scope Name
        /// </summary>
        public string ScopeId { get; set; }
    }
}
