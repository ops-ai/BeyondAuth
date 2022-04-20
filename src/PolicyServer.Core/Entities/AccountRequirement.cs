using BeyondAuth.PolicyServer.Core.Entities.AccountRequirements;
using JsonSubTypes;
using Newtonsoft.Json;

namespace BeyondAuth.PolicyServer.Core.Entities
{
    /// <summary>
    /// Authorization requirements base class
    /// </summary>
    [JsonConverter(typeof(JsonSubtypes), "name")]
    [JsonSubtypes.KnownSubType(typeof(LockoutRequirement), "complexity")]
    public abstract class AccountRequirement
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
