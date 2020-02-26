using System.Collections.Generic;

namespace PolicyServer.Core.Entities.AuthorizationRequirements
{
    /// <summary>
    /// Access is allowed during certain times
    /// </summary>
    public class TimeRequirement : AuthorizationRequirement
    {
        /// <summary>
        /// Unique name of requirement
        /// </summary>
        public override string Name => "time";

        /// <summary>
        /// Cron pattern to match
        /// </summary>
        public string Cron { get; set; }
    }
}
