using PolicyServer.Entities;
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
        public List<string> AuthenticationSchemes { get; set; }

        /// <summary>
        /// List of requirements the policy demands
        /// </summary>
        public List<AuthorizationRequirement> Requirements { get; set; }
    }
}
