namespace BeyondAuth.RelatedDataValidation.Requirements
{
    public interface IRequirementRule
    {
        /// <summary>
        /// The property this rule applies to
        /// </summary>
        string PropertyName { get; set; }
    }
}
