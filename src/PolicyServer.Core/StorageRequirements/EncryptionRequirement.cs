namespace PolicyServer.Core.Entities.AuthorizationRequirements
{
    /// <summary>
    /// Data must be encrypted at rest
    /// </summary>
    public class EncryptionRequirement : StorageRequirement
    {
        /// <summary>
        /// Unique name of requirement
        /// </summary>
        public override string Name => "encryption";
    }
}
