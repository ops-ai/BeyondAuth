using PolicyServer.Core.Entities;
using System.Collections;
using System.Collections.Generic;

namespace PolicyServer.Core.Models
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
        public string? Name { get; set; }

        /// <summary>
        /// Human/admin description of the policy
        /// </summary>
        public string? Description { get; set; }

        /// <summary>
        /// The resolution of this policy must be committed to the audit server
        /// </summary>
        public bool AuditableEvent { get; set; }

        /// <summary>
        /// Authentication schemes it applies to
        /// </summary>
        public List<string> AuthenticationSchemes { get; set; } = new List<string>();

        /// <summary>
        /// List of requirements the policy demands
        /// </summary>
        public List<AuthorizationRequirement> Requirements { get; set; } = new List<AuthorizationRequirement>();

        /// <summary>
        /// Type of policy. Ex: Named vs criteria-based
        /// </summary>
        public PolicyMatch Matching { get; set; }

        /// <summary>
        /// Infrastructure segment applicability of policy
        /// </summary>
        public PolicyApplicability Applicability { get; set; }

        /// <summary>
        /// Criteria the protected resource must match for this policy to apply
        /// </summary>
        public Hashtable? Criteria { get; set; }
    }
}
