namespace PolicyServer.Core.Entities.AuthorizationRequirements
{
    /// <summary>
    /// Minimum password length requirement
    /// </summary>
    public class MinPasswordLengthRequirement : PasswordRequirement
    {
        /// <summary>
        /// Unique name of requirement
        /// </summary>
        public override string Name => "min-length";

        /// <summary>
        /// Minimum length of password
        /// </summary>
        public ushort Length { get; set; }
    }
}
