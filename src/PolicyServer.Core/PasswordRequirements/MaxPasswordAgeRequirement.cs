namespace PolicyServer.Core.Entities.AuthorizationRequirements
{
    /// <summary>
    /// Maximum password age requirement
    /// </summary>
    public class MaxPasswordAgeRequirement : PasswordRequirement
    {
        /// <summary>
        /// Unique name of requirement
        /// </summary>
        public override string Name => "max-age";

        /// <summary>
        /// Number of days before the password expires
        /// </summary>
        public ushort Days { get; set; }
    }
}
