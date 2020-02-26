using System;

namespace PolicyServer.Core.Entities.AuthorizationRequirements
{
    /// <summary>
    /// Account requires MFA upon login
    /// </summary>
    public class MfaRequirement : AccountRequirement
    {
        /// <summary>
        /// Unique name of requirement
        /// </summary>
        public override string Name => "mfa";
    }
}
