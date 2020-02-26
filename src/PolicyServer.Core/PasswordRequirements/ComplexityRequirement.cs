namespace PolicyServer.Core.Entities.AuthorizationRequirements
{
    /// <summary>
    /// Complex password requirement
    /// </summary>
    public class ComplexityRequirement : PasswordRequirement
    {
        /// <summary>
        /// Unique name of requirement
        /// </summary>
        public override string Name => "complexity";
    }
}
