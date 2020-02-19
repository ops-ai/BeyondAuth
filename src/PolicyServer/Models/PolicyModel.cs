using PolicyServer.Core.Entities;
using System.Collections.Generic;

namespace PolicyServer.Models
{
    /// <summary>
    /// Policy details
    /// </summary>
    public class PolicyModel
    {
        /// <summary>
        /// Policy unique id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Policy name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Human/admin description of the policy
        /// </summary>
        public string Description { get; set; }

        /// <summary>
        /// Authentication schemes it applies to
        /// </summary>
        public List<string> AuthenticationSchemes { get; set; } = new List<string>();

        /// <summary>
        /// List of requirements the policy demands
        /// </summary>
        public List<AuthorizationRequirement> Requirements { get; set; } = new List<AuthorizationRequirement>();

        /// <summary>
        /// Criteria the protected resource must match for this policy to apply
        /// </summary>
        public List<KeyValuePair<string, string>> Criteria { get; set; }
    }
}
