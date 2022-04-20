namespace BeyondAuth.PolicyServer.Core.Entities.AccountRequirements
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
