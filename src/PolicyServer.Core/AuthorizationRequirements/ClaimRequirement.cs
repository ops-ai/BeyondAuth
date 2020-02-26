using System.Collections.Generic;

namespace PolicyServer.Core.Entities.AuthorizationRequirements
{
    /// <summary>
    /// Claim requirement
    /// </summary>
    public class ClaimRequirement : AuthorizationRequirement
    {
        /// <summary>
        /// Unique name of requirement
        /// </summary>
        public override string Name => "claim";

        /// <summary>
        /// Claim type the user must have
        /// </summary>
        public string ClaimType { get; set; }

        /// <summary>
        /// Values the claim must contain
        /// </summary>
        public List<string> RequiredValues { get; set; }
    }
}
