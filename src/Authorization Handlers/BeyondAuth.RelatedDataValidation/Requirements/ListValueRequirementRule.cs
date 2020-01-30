using System;
using System.Collections.Generic;
using System.Linq;

namespace BeyondAuth.RelatedDataValidation.Requirements
{
    public class ListValueRequirementRule : IRequirementRule, IEquatable<ListValueRequirementRule>
    {
        /// <summary>
        /// The property this rule applies to
        /// </summary>
        public string PropertyName { get; set; }

        /// <summary>
        /// Allowed values
        /// </summary>
        public List<string> Values { get; set; }

        public bool Equals(ListValueRequirementRule other)
        {
            return PropertyName == other.PropertyName && Values.Count == other.Values.Count && Values.Intersect(other.Values).Count() == Values.Count;
        }
    }
}
