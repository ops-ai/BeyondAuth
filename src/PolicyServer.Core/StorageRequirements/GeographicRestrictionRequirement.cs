using System.Collections.Generic;

namespace PolicyServer.Core.Entities.StorageRequirements
{
    /// <summary>
    /// Data must be stored only in one of the allowed zones
    /// </summary>
    public class GeographicRestrictionRequirement : StorageRequirement
    {
        /// <summary>
        /// Unique name of requirement
        /// </summary>
        public override string Name => "geographic-restriction";

        /// <summary>
        /// Zones where data is allowed to be stored
        /// </summary>
        public List<string> AllowedZones { get; set; }
    }
}
