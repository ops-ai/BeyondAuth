namespace BeyondAuth.PolicyServer.Core.Entities.AuthorizationRequirements
{
    /// <summary>
    /// Group membership requirement
    /// </summary>
    public class GroupMembershipRequirement : AuthorizationRequirement
    {
        /// <summary>
        /// Unique name of requirement
        /// </summary>
        public override string Name => "group";

        /// <summary>
        /// Group the user must be a member of
        /// </summary>
        public string GroupName { get; set; }
    }
}
