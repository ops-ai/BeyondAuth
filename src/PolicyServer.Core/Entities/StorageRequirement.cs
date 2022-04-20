using BeyondAuth.PolicyServer.Core.Entities.StorageRequirements;
using JsonSubTypes;
using Newtonsoft.Json;

namespace BeyondAuth.PolicyServer.Core.Entities
{
    /// <summary>
    /// Authorization requirements base class
    /// </summary>
    [JsonConverter(typeof(JsonSubtypes), "name")]
    [JsonSubtypes.KnownSubType(typeof(EncryptionRequirement), "encryption")]
    [JsonSubtypes.KnownSubType(typeof(GeographicRestrictionRequirement), "geographic-restriction")]
    [JsonSubtypes.KnownSubType(typeof(ReplicationRequirement), "replication")]
    public abstract class StorageRequirement
    {
        /// <summary>
        /// Unique name of requirement
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Minimum version of the requirement to verify against
        /// </summary>
        public string MinVer { get; }

        /// <summary>
        /// Returns the requirement name and class type
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"{Name}-{GetType()}";
    }
}
