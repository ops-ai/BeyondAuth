using System;

namespace BeyondAuth.RelatedDataValidation.Requirements
{
    public class SingleValueRequirementRule : IRequirementRule, IEquatable<SingleValueRequirementRule>
    {
        /// <summary>
        /// The property this rule applies to
        /// </summary>
        public string PropertyName { get; set; }

        public bool Equals(SingleValueRequirementRule other) => other.PropertyName == PropertyName;
    }
}
