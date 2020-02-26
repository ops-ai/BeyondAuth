﻿namespace PolicyServer.Core.Entities.AuthorizationRequirements
{
    /// <summary>
    /// Replication requirement
    /// </summary>
    public class ReplicationRequirement : StorageRequirement
    {
        /// <summary>
        /// Unique name of requirement
        /// </summary>
        public override string Name => "replication";

        /// <summary>
        /// Minimum replicas data must have at all times
        /// </summary>
        public ushort MinReplicas { get; set; }

        /// <summary>
        /// Minimum number of different storage providers data must be replicated on
        /// </summary>
        public ushort MinDataSourceTypes { get; set; }
    }
}
