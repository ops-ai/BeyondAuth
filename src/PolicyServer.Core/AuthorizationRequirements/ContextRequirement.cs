namespace PolicyServer.Core.Entities.AuthorizationRequirements
{
    /// <summary>
    /// Context requirement
    /// </summary>
    public class ContextRequirement : AuthorizationRequirement
    {
        /// <summary>
        /// Unique name of requirement
        /// </summary>
        public override string Name => "context";

        /// <summary>
        /// Scope that must be present
        /// </summary>
        public string ContextName { get; set; }
    }
}
