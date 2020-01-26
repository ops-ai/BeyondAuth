using BeyondAuth.RelatedDataValidation.Requirements;
using System.Collections.Generic;

namespace BeyondAuth.RelatedDataValidation
{
    public class RelatedDataValidationRule
    {
        /// <summary>
        /// Unique rule identifier
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        /// Rule display name
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Conditions that must be met for this rule to apply
        /// Ex: Applies when Organization is ClientA
        /// </summary>
        public Dictionary<string, string> Conditions { get; set; }

        /// <summary>
        /// Requirements the cummulative property set must meet
        /// Ex: Organization must be one of ClientA ClientA1 MyOrg
        /// Ex2: Organization property must contain only 1 value
        /// </summary>
        public List<IRequirementRule> Requirements { get; set; }
    }
}
