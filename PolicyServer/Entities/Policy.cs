using System.Collections.Generic;

namespace PolicyServer.Entities
{
    public class Policy
    {
        /// <summary>
        /// Policy unique id
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Client Id the policy is created for
        /// </summary>
        public string ClientId { get; set; }

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
        public List<string> AuthenticationSchemes { get; } = new List<string>();

        /// <summary>
        /// List of requirements the policy demands
        /// </summary>
        public List<AuthorizationRequirement> Requirements { get; set; } = new List<AuthorizationRequirement>();
    }
}
