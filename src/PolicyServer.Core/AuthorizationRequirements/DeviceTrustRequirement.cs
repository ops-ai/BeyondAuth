using System.Collections.Generic;

namespace PolicyServer.Core.Entities.AuthorizationRequirements
{
    /// <summary>
    /// Device trust requirement
    /// </summary>
    public class DeviceRequirement : AuthorizationRequirement
    {
        /// <summary>
        /// Unique name of requirement
        /// </summary>
        public override string Name => "device";

        /// <summary>
        /// Minimum device trust level
        /// </summary>
        public ushort MinDeviceScore { get; set; }
    }
}
