namespace BeyondAuth.PolicyServer.Core.Entities.AuthorizationRequirements
{
    /// <summary>
    /// Role membership requirement
    /// </summary>
    public class RoleMembershipRequirement : AuthorizationRequirement
    {
        /// <summary>
        /// Unique name of requirement
        /// </summary>
        public override string Name => "role";

        /// <summary>
        /// Role the user must be a member of
        /// </summary>
        public string RoleName { get; set; }
    }
}
