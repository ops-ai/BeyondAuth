namespace BeyondAuth.PolicyServer.Core.Entities.AuthorizationRequirements
{
    /// <summary>
    /// Location trust requirement
    /// </summary>
    public class LocationTrustRequirement : AuthorizationRequirement
    {
        /// <summary>
        /// Unique name of requirement
        /// </summary>
        public override string Name => "location";

        /// <summary>
        /// Minimum location trust level
        /// Unknwon / first time seen -> Known Corporate / trusted location -> Usual activity from location
        /// </summary>
        public ushort MinLocationScore { get; set; }
    }
}
