namespace BeyondAuth.PolicyServer.Core.Entities.AuthorizationRequirements
{
    /// <summary>
    /// Authenticated user requirement
    /// </summary>
    public class AuthenticatedRequirement : AuthorizationRequirement
    {
        /// <summary>
        /// Unique name of requirement
        /// </summary>
        public override string Name => "authenticated";
    }
}
