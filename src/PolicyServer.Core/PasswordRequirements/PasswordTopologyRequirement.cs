namespace BeyondAuth.PolicyServer.Core.Entities.PasswordRequirements
{
    /// <summary>
    /// Topology requirement
    /// </summary>
    public class PasswordTopologyRequirement : PasswordRequirement
    {
        /// <summary>
        /// Unique name of requirement
        /// </summary>
        public override string Name => "topology";

        /// <summary>
        /// Number of top topologies to block
        /// </summary>
        public ushort TopologiesBlocked { get; set; }
    }
}
