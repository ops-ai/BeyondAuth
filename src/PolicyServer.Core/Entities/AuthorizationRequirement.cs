using JsonSubTypes;
using Microsoft.AspNetCore.Authorization;
using Newtonsoft.Json;
using NJsonSchema.Converters;
using BeyondAuth.PolicyServer.Core.Entities.AuthorizationRequirements;

namespace BeyondAuth.PolicyServer.Core.Entities
{
    /// <summary>
    /// Authorization requirements base class
    /// </summary>
    //[JsonConverter(typeof(JsonInheritanceConverter), "name")]
    [JsonConverter(typeof(JsonSubtypes), "name")]
    [JsonSubtypes.KnownSubType(typeof(AuthenticatedRequirement), "authenticated")]
    [JsonSubtypes.KnownSubType(typeof(ClaimRequirement), "claim")]
    [JsonSubtypes.KnownSubType(typeof(GroupMembershipRequirement), "group")]
    [JsonSubtypes.KnownSubType(typeof(RoleMembershipRequirement), "role")]
    [JsonSubtypes.KnownSubType(typeof(ScopeRequirement), "scope")]
    [JsonSubtypes.KnownSubType(typeof(UsernameRequirement), "username")]
    [JsonSubtypes.KnownSubType(typeof(MfaRequirement), "mfa")]
    public abstract class AuthorizationRequirement : IAuthorizationRequirement
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
