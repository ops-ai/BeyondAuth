using Audit.Core;

namespace IdentityManager.Data.Audit
{
    public class GroupEvent : AuditEvent
    {
        /// <summary>
        /// Id of team that changed
        /// </summary>
        public string GroupId { get; set; }

        /// <summary>
        /// User Id
        /// </summary>
        public string UserId { get; set; }

        /// <summary>
        /// User agent
        /// </summary>
        public string? UserAgent { get; set; }

        /// <summary>
        /// IP Address
        /// </summary>
        public string? RemoteIpAddress { get; set; }
    }
}
