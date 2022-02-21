namespace BeyondAuth.PolicyServer.Core.Entities.PasswordRequirements
{
    /// <summary>
    /// Prevent passwords found in data breaches from being used
    /// </summary>
    public class LeakedPasswordRequirement : PasswordRequirement
    {
        /// <summary>
        /// Unique name of requirement
        /// </summary>
        public override string Name => "leaked-passwords";

        /// <summary>
        /// Number of top passwords from breaches to block
        /// </summary>
        public ushort TopPasswords { get; set; }
    }
}
