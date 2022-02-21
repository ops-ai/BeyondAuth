using System.Collections.Generic;

namespace BeyondAuth.PolicyServer.Core.Entities.AuthorizationRequirements
{
    /// <summary>
    /// MFA requirement
    /// </summary>
    public class MfaRequirement : AuthorizationRequirement
    {
        /// <summary>
        /// Unique name of requirement
        /// </summary>
        public override string Name => "mfa";

        /// <summary>
        /// Allowed MFA method
        /// </summary>
        public List<string> AllowedMethods { get; set; }
    }
}
