namespace PolicyServer.Core.Entities.RoutingRequirements
{
    /// <summary>
    /// Data must be encrypted at rest
    /// </summary>
    public class NextHopRequirement : StorageRequirement
    {
        /// <summary>
        /// Unique name of requirement
        /// </summary>
        public override string Name => "next-hop";

        /// <summary>
        /// Next hop
        /// </summary>
        public byte[] NextHop { get; set; }
    }
}
