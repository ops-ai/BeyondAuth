using System.Collections.Generic;

namespace BeyondAuth.RelatedDataValidation.Requirements
{
    public class ListValueRequirementRule : IRequirementRule
    {
        /// <summary>
        /// The property this rule applies to
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// Allowed values
        /// </summary>
        public List<string> Values { get; set; }
    }
}
