using JsonSubTypes;
using Newtonsoft.Json;
using NJsonSchema.Converters;
using BeyondAuth.PolicyServer.Core.Entities.PasswordRequirements;

namespace BeyondAuth.PolicyServer.Core.Entities
{
    /// <summary>
    /// Authorization requirements base class
    /// </summary>
    [JsonConverter(typeof(JsonSubtypes), "name")]
    [JsonSubtypes.KnownSubType(typeof(ComplexityRequirement), "complexity")]
    [JsonSubtypes.KnownSubType(typeof(MinPasswordAgeRequirement), "min-age")]
    [JsonSubtypes.KnownSubType(typeof(MaxPasswordAgeRequirement), "max-age")]
    [JsonSubtypes.KnownSubType(typeof(MinPasswordLengthRequirement), "min-length")]
    [JsonSubtypes.KnownSubType(typeof(PasswordDictionaryRequirement), "dictionary")]
    [JsonSubtypes.KnownSubType(typeof(PasswordHistoryRequirement), "history")]
    [JsonSubtypes.KnownSubType(typeof(LeakedPasswordRequirement), "leaked-passwords")]
    [JsonSubtypes.KnownSubType(typeof(PasswordTopologyRequirement), "topology")]
    public abstract class PasswordRequirement
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
