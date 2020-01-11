using JsonSubTypes;
using Newtonsoft.Json;
using NJsonSchema.Converters;
using PolicyServer.Entities.Requirements;

namespace PolicyServer.Entities
{
    /// <summary>
    /// Authorization requirements base class
    /// </summary>
    [JsonConverter(typeof(JsonInheritanceConverter), "name")]
    [JsonSubtypes.KnownSubType(typeof(AuthenticatedRequirement), "authenticated")]
    public abstract class AuthorizationRequirement
    {
        /// <summary>
        /// Unique name of requirement
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// Returns the requirement name and class type
        /// </summary>
        /// <returns></returns>
        public override string ToString() => $"{Name}-{GetType()}";
    }
}
