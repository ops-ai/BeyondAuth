namespace BeyondAuth.PolicyServer.Core.Entities.AuthorizationRequirements
{
    /// <summary>
    /// Identity verification strength requirement
    /// </summary>
    public class IdentityRequirement : AuthorizationRequirement
    {
        /// <summary>
        /// Unique name of requirement
        /// </summary>
        public override string Name => "identity";

        /// <summary>
        /// Minimum identity verification stength
        /// </summary>
        public ushort IdentityStrength { get; set; }
    }
}
