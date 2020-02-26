namespace PolicyServer.Core.Entities.AuthorizationRequirements
{
    /// <summary>
    /// Scope requirement
    /// </summary>
    public class ScopeRequirement : AuthorizationRequirement
    {
        /// <summary>
        /// Unique name of requirement
        /// </summary>
        public override string Name => "scope";

        /// <summary>
        /// Scope that must be present
        /// </summary>
        public string ScopeName { get; set; }
    }
}
