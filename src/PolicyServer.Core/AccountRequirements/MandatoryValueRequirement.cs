using System;
using System.Collections.Generic;

namespace PolicyServer.Core.Entities.AuthorizationRequirements
{
    /// <summary>
    /// Certain fields must contain specific values
    /// ex: Password change not allowed
    /// </summary>
    public class MandatoryValueRequirement : AccountRequirement
    {
        /// <summary>
        /// Unique name of requirement
        /// </summary>
        public override string Name => "mandatory-value";

        /// <summary>
        /// Mapping of properties to values they must match
        /// </summary>
        public Dictionary<string, object> Values { get; set; }
    }
}
