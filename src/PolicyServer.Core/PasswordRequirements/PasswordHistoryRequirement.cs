namespace PolicyServer.Core.Entities.AuthorizationRequirements
{
    /// <summary>
    /// Minimum password length requirement
    /// </summary>
    public class PasswordHistoryRequirement : PasswordRequirement
    {
        /// <summary>
        /// Unique name of requirement
        /// </summary>
        public override string Name => "history";

        /// <summary>
        /// Number of previous passwords remembered
        /// </summary>
        public ushort RememberedPasswords { get; set; }
    }
}
