﻿using System.Collections.Generic;

namespace PolicyServer.Core.Entities
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
        /// The resolution of this policy must be committed to the audit server
        /// </summary>
        public bool AuditableEvent { get; set; }

        /// <summary>
        /// Authentication schemes it applies to
        /// </summary>
        public List<string> AuthenticationSchemes { get; } = new List<string>();

        /// <summary>
        /// List of requirements the policy demands
        /// </summary>
        public List<AuthorizationRequirement> Requirements { get; set; } = new List<AuthorizationRequirement>();

        /// <summary>
        /// Type of policy. Ex: Named vs criteria-based
        /// </summary>
        public PolicyTypes PolicyType { get; set; }

        /// <summary>
        /// Criteria the protected resource must match for this policy to apply
        /// </summary>
        public List<KeyValuePair<string, string>> Criteria { get; set; }
    }
}
