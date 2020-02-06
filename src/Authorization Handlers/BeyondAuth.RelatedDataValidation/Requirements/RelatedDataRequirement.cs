using PolicyServer.Core.Entities;

namespace BeyondAuth.RelatedDataValidation.Requirements
{
    public class RelatedDataRequirement : AuthorizationRequirement
    {
        public RelatedDataRequirement()
        {

        }

        public override string Name => "related-data";
    }
}
