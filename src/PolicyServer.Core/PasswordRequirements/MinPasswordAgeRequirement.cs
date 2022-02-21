namespace BeyondAuth.PolicyServer.Core.Entities.PasswordRequirements
{
    /// <summary>
    /// Minimum password age requirement
    /// </summary>
    public class MinPasswordAgeRequirement : PasswordRequirement
    {
        /// <summary>
        /// Unique name of requirement
        /// </summary>
        public override string Name => "min-age";

        /// <summary>
        /// Mimimum number of days before the password can be changed again
        /// </summary>
        public ushort Days { get; set; }
    }
}
